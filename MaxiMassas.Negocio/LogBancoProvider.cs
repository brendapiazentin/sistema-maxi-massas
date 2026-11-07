// Grava no banco (tabela LogsSistema) as mensagens de log do sistema, para a equipe consultar
// o que aconteceu (erros, pedidos do app, falhas de envio) sem entrar no servidor.
//
// Como funciona: o código já chama _logger.LogXxx(...) em vários pontos. Este provider "escuta"
// essas chamadas e guarda numa fila; um gravador em segundo plano salva a fila no banco.
// O log NUNCA pode derrubar o sistema: qualquer falha ao gravar é descartada em silêncio.
//
// Entram: mensagens das classes do próprio sistema (MaxiMassas.*) de nível Information ou acima,
// e erros não tratados do ASP.NET (nível Error). Ficam de fora os logs do EF Core e do restante
// do ASP.NET (evita laço infinito e excesso de volume).
using System.Threading.Channels;
using MaxiMassas.Modelos;
using MaxiMassas.Repositorio.Contexto;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Negocio;

public static class LogBancoExtensions
{
    /// <param name="origem">"Web" (painel) ou "Api" (inclui o app de pedidos).</param>
    public static ILoggingBuilder AddLogBanco(this ILoggingBuilder logging, string connectionString, string origem)
    {
        logging.AddProvider(new LogBancoProvider(connectionString, origem));
        return logging;
    }
}

public sealed class LogBancoProvider : ILoggerProvider
{
    private const int TamanhoFila = 1000;
    private const int TamanhoLote = 50;

    private static readonly TimeZoneInfo? FusoBrasilia = ObterFusoBrasilia();

    private readonly string _origem;
    private readonly DbContextOptions<AppDbContext> _opcoes;
    private readonly Channel<LogSistema> _fila = Channel.CreateBounded<LogSistema>(
        new BoundedChannelOptions(TamanhoFila) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    private readonly Task _gravador;

    public LogBancoProvider(string connectionString, string origem)
    {
        _origem = origem;
        // Contexto próprio e sem logger do EF: gravar o log não gera novos logs.
        _opcoes = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        _gravador = Task.Run(GravarEmSegundoPlanoAsync);
    }

    public ILogger CreateLogger(string categoryName) => new LogBanco(this, categoryName);

    public void Dispose()
    {
        _fila.Writer.TryComplete();
        try { _gravador.Wait(TimeSpan.FromSeconds(5)); } catch { /* encerrando: nada a fazer */ }
    }

    private async Task GravarEmSegundoPlanoAsync()
    {
        var lote = new List<LogSistema>(TamanhoLote);
        try
        {
            await foreach (var primeiro in _fila.Reader.ReadAllAsync())
            {
                lote.Add(primeiro);
                while (lote.Count < TamanhoLote && _fila.Reader.TryRead(out var proximo))
                    lote.Add(proximo);

                await SalvarAsync(lote);
                lote.Clear();
            }
        }
        catch { /* o log nunca derruba o sistema */ }
    }

    private async Task SalvarAsync(List<LogSistema> lote)
    {
        try
        {
            await using var db = new AppDbContext(_opcoes);
            db.LogsSistema.AddRange(lote);
            await db.SaveChangesAsync();
        }
        catch { /* banco fora do ar ou tabela ainda não criada: descarta este lote */ }
    }

    private static bool Aceita(string categoria, LogLevel nivel)
    {
        if (categoria.StartsWith("MaxiMassas.", StringComparison.Ordinal)) return nivel >= LogLevel.Information;
        if (categoria.StartsWith("Microsoft.AspNetCore.Diagnostics", StringComparison.Ordinal)) return nivel >= LogLevel.Error;
        return false;
    }

    private static string Cortar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..maximo];

    private static TimeZoneInfo? ObterFusoBrasilia()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
        }
        return null;
    }

    // O servidor roda em UTC; o log sempre sai no horário de Brasília.
    private static DateTime AgoraBrasilia() =>
        FusoBrasilia is null ? DateTime.Now : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, FusoBrasilia);

    private sealed class LogBanco : ILogger
    {
        private readonly LogBancoProvider _provider;
        private readonly string _categoria;

        public LogBanco(LogBancoProvider provider, string categoria)
        {
            _provider = provider;
            _categoria = categoria;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && Aceita(_categoria, logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            try
            {
                var categoria = _categoria.StartsWith("MaxiMassas.", StringComparison.Ordinal)
                    ? _categoria["MaxiMassas.".Length..]
                    : _categoria;
                var detalhe = exception?.ToString();

                _provider._fila.Writer.TryWrite(new LogSistema
                {
                    DataHora = AgoraBrasilia(),
                    Nivel = logLevel switch
                    {
                        LogLevel.Warning => "Aviso",
                        LogLevel.Error or LogLevel.Critical => "Erro",
                        _ => "Info"
                    },
                    Origem = _provider._origem,
                    Categoria = Cortar(categoria, 150),
                    Mensagem = Cortar(formatter(state, exception), 2000),
                    Excecao = detalhe is null ? null : Cortar(detalhe, 4000)
                });
            }
            catch { /* o log nunca derruba o sistema */ }
        }
    }
}

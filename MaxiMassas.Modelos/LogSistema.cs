// Registro de eventos e erros do sistema (Web e Api/app), gravado pelo LogBancoProvider.
// Serve para acompanhar problemas, por exemplo quando um cliente não consegue concluir um pedido no app.
namespace MaxiMassas.Modelos;

public class LogSistema
{
    public long Id { get; set; }

    /// <summary>Data/hora no horário de Brasília.</summary>
    public DateTime DataHora { get; set; }

    /// <summary>Info, Aviso ou Erro.</summary>
    public string Nivel { get; set; } = string.Empty;

    /// <summary>"Web" (painel) ou "Api" (inclui os pedidos feitos pelo app).</summary>
    public string Origem { get; set; } = string.Empty;

    /// <summary>Classe que registrou a mensagem (ex.: Negocio.AppClienteNegocio).</summary>
    public string Categoria { get; set; } = string.Empty;

    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Detalhe técnico (stack trace) quando houve exceção.</summary>
    public string? Excecao { get; set; }
}

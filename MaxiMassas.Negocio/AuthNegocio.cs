// US16 — "Esqueci minha senha", para os dois públicos do sistema:
//
//   PAINEL (Usuario): e-mail ou login → link por e-mail (token aleatório de 256 bits), válido por 1 hora,
//                     que abre Pages/RedefinirSenha. Usuário sem e-mail cadastrado: só o administrador redefine.
//   APP (Cliente):    e-mail → código de 6 dígitos por e-mail, válido por 15 minutos, digitado no app;
//                     no máximo 5 tentativas erradas por código (não dá para adivinhar no chute).
//
// Comum aos dois: resposta sempre igual (não revela quem tem conta), no máximo um e-mail por minuto para a
// mesma pessoa, uso único, pedido novo invalida o anterior, e no banco fica só o hash SHA-256 do token/código.
using System.Security.Cryptography;
using System.Text;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Validadores;

namespace MaxiMassas.Negocio;

public interface IAuthNegocio
{
    /// <summary>Painel, passo 1: gera o link e envia por e-mail (se o usuário existir, estiver ativo e tiver e-mail).</summary>
    Task SolicitarResetSenhaAsync(string emailOuLogin);

    /// <summary>Painel: o link ainda vale? (para a página mostrar o formulário ou "link expirado").</summary>
    Task<bool> TokenValidoAsync(string token);

    /// <summary>Painel, passo 2: troca a senha com o token do link. InvalidOperationException com a mensagem para a tela.</summary>
    Task RedefinirSenhaAsync(ResetPasswordRequest request);

    /// <summary>App, passo 1: envia o código de 6 dígitos (se houver cliente com esse e-mail).</summary>
    Task SolicitarCodigoClienteAsync(string email);

    /// <summary>App, passo 2: confere o código e troca a senha. InvalidOperationException com a mensagem para a tela.</summary>
    Task RedefinirSenhaClienteAsync(RedefinirSenhaAppRequest request);
}

public class AuthNegocio : IAuthNegocio
{
    private static readonly TimeSpan ValidadeLink = TimeSpan.FromHours(1);
    private static readonly TimeSpan ValidadeCodigo = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan IntervaloEntreEnvios = TimeSpan.FromMinutes(1);
    private const int MaximoTentativasCodigo = 5;

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IClienteRepositorio _clientes;
    private readonly IRedefinicaoSenhaRepositorio _tokens;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthNegocio> _logger;
    private readonly string _urlBase;

    public AuthNegocio(IUsuarioRepositorio usuarios, IClienteRepositorio clientes, IRedefinicaoSenhaRepositorio tokens, IEmailService emailService, IConfiguration config, ILogger<AuthNegocio> logger)
    {
        _usuarios = usuarios;
        _clientes = clientes;
        _tokens = tokens;
        _emailService = emailService;
        _logger = logger;
        _urlBase = (config["EmailSettings:UrlBase"] ?? "https://localhost:5001").TrimEnd('/');
    }

    // ── Painel ───────────────────────────────────────────────────────────────

    public async Task SolicitarResetSenhaAsync(string emailOuLogin)
    {
        var texto = (emailOuLogin ?? string.Empty).Trim().ToLowerInvariant();
        if (texto.Length == 0) return;

        var usuario = await _usuarios.GetByEmailOuLoginAsync(texto);

        if (usuario is null || !usuario.Ativo || string.IsNullOrEmpty(usuario.Email))
        {
            // Não revela nada a quem pediu; só registra para diagnóstico.
            _logger.LogWarning("[ResetSenha] Pedido sem envio (inexistente, desativado ou sem e-mail): {Texto}", texto);
            return;
        }

        if (!await PodeEnviarAsync(DestinoRedefinicao.Usuario, usuario.Id)) return;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_'); // base64 seguro para URL
        await RegistrarAsync(DestinoRedefinicao.Usuario, usuario.Id, usuario.Email, token, ValidadeLink);

        var link = $"{_urlBase}/RedefinirSenha?token={Uri.EscapeDataString(token)}";
        await EnviarSemDerrubarAsync(() => _emailService.EnviarLinkRedefinicaoAsync(usuario.Email, usuario.Nome, link), usuario.Email);
        _logger.LogInformation("[ResetSenha] Link enviado para o usuário {Login}.", usuario.Login);
    }

    public async Task<bool> TokenValidoAsync(string token)
        => !string.IsNullOrWhiteSpace(token) && await BuscarTokenAbertoAsync(token) is not null;

    public async Task RedefinirSenhaAsync(ResetPasswordRequest request)
    {
        if (request.NovaSenha != request.ConfirmarSenha)
            throw new InvalidOperationException("A nova senha e a confirmação não coincidem.");
        var erroSenha = PoliticaSenha.Validar(request.NovaSenha);
        if (erroSenha is not null) throw new InvalidOperationException(erroSenha);

        var registro = await BuscarTokenAbertoAsync(request.Token)
            ?? throw new InvalidOperationException("Este link expirou ou já foi usado. Peça um novo em \"Esqueci minha senha\".");

        var usuario = await _usuarios.GetByIdAsync(registro.ReferenciaId);
        if (usuario is null || !usuario.Ativo)
            throw new InvalidOperationException("Este usuário não pode mais acessar o sistema. Fale com o administrador.");

        usuario.Senha = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        await InvalidarAbertosAsync(DestinoRedefinicao.Usuario, usuario.Id); // inclui o próprio registro
        await _tokens.SaveChangesAsync();
        _logger.LogInformation("[ResetSenha] Senha redefinida pelo link para o usuário {Login}.", usuario.Login);
    }

    private async Task<PasswordResetToken?> BuscarTokenAbertoAsync(string token)
    {
        var hash = Hash(token ?? string.Empty);
        return await _tokens.GetTokenAbertoUsuarioAsync(hash);
    }

    // ── App (clientes) ───────────────────────────────────────────────────────

    public async Task SolicitarCodigoClienteAsync(string email)
    {
        var texto = (email ?? string.Empty).Trim().ToLowerInvariant();
        var cliente = texto.Length == 0 ? null
            : await _clientes.GetContaAppPorEmailAsync(texto);

        if (cliente is null)
        {
            _logger.LogWarning("[ResetSenha:App] Pedido para e-mail sem conta no app: {Email}", texto);
            return;
        }

        if (!await PodeEnviarAsync(DestinoRedefinicao.Cliente, cliente.Id)) return;

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await RegistrarAsync(DestinoRedefinicao.Cliente, cliente.Id, cliente.Email!, CodigoParaHash(cliente.Id, codigo), ValidadeCodigo);
        await EnviarSemDerrubarAsync(() => _emailService.EnviarCodigoRedefinicaoAsync(cliente.Email!, cliente.Nome, codigo), cliente.Email!);
        _logger.LogInformation("[ResetSenha:App] Código enviado para o cliente {ClienteId}.", cliente.Id);
    }

    public async Task RedefinirSenhaClienteAsync(RedefinirSenhaAppRequest request)
    {
        var erroSenha = PoliticaSenha.Validar(request.NovaSenha);
        if (erroSenha is not null) throw new InvalidOperationException(erroSenha);

        const string semCodigo = "Código inválido ou expirado. Peça um novo código.";
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var cliente = await _clientes.GetContaAppPorEmailAsync(email)
            ?? throw new InvalidOperationException(semCodigo);

        var registro = await _tokens.GetCodigoAbertoClienteAsync(cliente.Id)
            ?? throw new InvalidOperationException(semCodigo);

        var codigo = new string((request.Codigo ?? string.Empty).Where(char.IsDigit).ToArray());
        var confere = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(CodigoParaHash(cliente.Id, codigo))), Encoding.ASCII.GetBytes(registro.Token));
        if (!confere)
        {
            registro.Tentativas++;
            var esgotou = registro.Tentativas >= MaximoTentativasCodigo;
            if (esgotou) registro.Usado = true;
            await _tokens.SaveChangesAsync();
            throw new InvalidOperationException(esgotou
                ? "Muitas tentativas erradas. Peça um novo código."
                : $"Código incorreto. Você ainda tem {MaximoTentativasCodigo - registro.Tentativas} tentativa(s).");
        }

        cliente.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha);
        await InvalidarAbertosAsync(DestinoRedefinicao.Cliente, cliente.Id);
        await _tokens.SaveChangesAsync();
        _logger.LogInformation("[ResetSenha:App] Senha redefinida pelo código para o cliente {ClienteId}.", cliente.Id);
    }

    // ── Comum ────────────────────────────────────────────────────────────────

    /// <summary>No máximo um e-mail por minuto para a mesma pessoa (evita encher a caixa de alguém de propósito).</summary>
    private async Task<bool> PodeEnviarAsync(DestinoRedefinicao destino, int referenciaId)
    {
        var limite = DateTime.Now - IntervaloEntreEnvios;
        var recente = await _tokens.HouvePedidoDepoisDeAsync(destino, referenciaId, limite);
        if (recente) _logger.LogInformation("[ResetSenha] Pedido repetido em menos de 1 minuto ignorado ({Destino} {Id}).", destino, referenciaId);
        return !recente;
    }

    /// <summary>Invalida pedidos anteriores ainda abertos e guarda o novo (só o hash).</summary>
    private async Task RegistrarAsync(DestinoRedefinicao destino, int referenciaId, string email, string segredo, TimeSpan validade)
    {
        await InvalidarAbertosAsync(destino, referenciaId);
        await _tokens.AddAsync(new PasswordResetToken
        {
            Destino = destino,
            ReferenciaId = referenciaId,
            Email = email,
            Token = Hash(segredo),
            ExpiresAt = DateTime.Now + validade,
            CriadoEm = DateTime.Now
        });
        await _tokens.SaveChangesAsync();
    }

    private async Task InvalidarAbertosAsync(DestinoRedefinicao destino, int referenciaId)
    {
        await _tokens.InvalidarAbertosAsync(destino, referenciaId);
    }

    private async Task EnviarSemDerrubarAsync(Func<Task> envio, string email)
    {
        try { await envio(); }
        catch (Exception ex)
        {
            // Falha de SMTP não pode revelar nada nem derrubar a tela; fica no log para o administrador.
            _logger.LogError(ex, "[ResetSenha] Falha no envio de e-mail para {Email}", email);
        }
    }

    /// <summary>O código de 6 dígitos entra no hash junto com o id do cliente (o mesmo código de clientes diferentes gera hashes diferentes).</summary>
    private static string CodigoParaHash(int clienteId, string codigo) => $"cliente:{clienteId}:{codigo}";

    private static string Hash(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
}

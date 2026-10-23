// Autenticação Basic Auth dos usuários internos (painel/integrações) na Api.
// Fluxo: header -> decodifica base64 -> confere login/senha na camada de Negócio -> claims.
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MaxiMassas.Api.Seguranca;

public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "BasicAuth";

    private readonly IAutenticacaoNegocio _autenticacao;

    public BasicAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder,
        IAutenticacaoNegocio autenticacao)
        : base(options, logger, encoder)
    { _autenticacao = autenticacao; }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return AuthenticateResult.Fail("Header Authorization ausente.");
        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]!);
            if (!authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.Fail("Scheme invalido. Use Basic.");

            var creds = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter ?? string.Empty));
            var sep = creds.IndexOf(':');
            if (sep < 0) return AuthenticateResult.Fail("Formato de credenciais invalido.");

            var usuario = await _autenticacao.AutenticarUsuarioAsync(creds[..sep], creds[(sep + 1)..]);
            if (usuario is null) return AuthenticateResult.Fail("Login ou senha invalidos.");

            var principal = PrincipalUsuario.Criar(usuario, Scheme.Name);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro na autenticacao Basic Auth.");
            return AuthenticateResult.Fail("Erro interno na autenticacao.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.Headers["WWW-Authenticate"] = "Basic realm=\"MaxiMassasAPI\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }
}

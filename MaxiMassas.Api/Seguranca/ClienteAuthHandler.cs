// Basic Auth dos CLIENTES do app (login por e-mail). Separado do BasicAuthHandler para que um
// cliente nunca acesse endpoints de gestão, e vice-versa. Usado apenas pelos endpoints /api/app.
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MaxiMassas.Api.Seguranca;

public class ClienteAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ClienteAuth";

    private readonly IAutenticacaoNegocio _autenticacao;

    public ClienteAuthHandler(
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

            var cliente = await _autenticacao.AutenticarClienteAsync(creds[..sep], creds[(sep + 1)..]);
            if (cliente is null) return AuthenticateResult.Fail("E-mail ou senha invalidos.");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
                new Claim(ClaimTypes.Email, cliente.Email!),
                new Claim(ClaimTypes.GivenName, cliente.Nome),
                new Claim(ClaimTypes.Role, "Cliente")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro na autenticacao do cliente.");
            return AuthenticateResult.Fail("Erro interno na autenticacao.");
        }
    }

    // Sem o header WWW-Authenticate: ele faz o navegador abrir a janela nativa de login,
    // o que atrapalha o app rodando na web. O app trata o 401 sozinho.
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }
}

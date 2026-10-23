// Autenticação do painel web: login em /Login cria um cookie de sessão (HttpOnly, SameSite=Strict).
// Quem confere usuário e senha é a camada de Negócio (IAutenticacaoNegocio); aqui só se configura o cookie.
using System.Security.Claims;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MaxiMassas.Web.Seguranca;

public static class PainelAuth
{
    /// <summary>Cookie de sessão do painel.</summary>
    public const string Scheme = "Painel";

    /// <summary>Política aplicada a todas as telas do painel (menos as de acesso).</summary>
    public const string Policy = "Painel";

    /// <summary>
    /// A cada requisição do painel: usuário excluído ou desativado perde a sessão na hora; e se o nome,
    /// o perfil ou a foto mudaram, o cookie é atualizado — o menu e as permissões refletem a mudança sem
    /// precisar sair e entrar de novo.
    /// </summary>
    private static async Task ValidarSessaoAsync(CookieValidatePrincipalContext ctx)
    {
        if (!int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            ctx.RejectPrincipal();
            return;
        }

        var autenticacao = ctx.HttpContext.RequestServices.GetRequiredService<IAutenticacaoNegocio>();
        var atual = await autenticacao.ObterResumoSessaoAsync(id);

        if (atual is null || !atual.Ativo)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(Scheme);
            return;
        }

        var p = ctx.Principal!;
        var mudou = p.FindFirstValue(ClaimTypes.GivenName) != atual.Nome
                    || p.FindFirstValue(ClaimTypes.Role) != atual.Tipo.ToString()
                    || p.FindFirstValue(PrincipalUsuario.ClaimFoto) != PrincipalUsuario.VersaoFoto(atual.FotoAtualizadaEm);
        if (mudou)
        {
            ctx.ReplacePrincipal(PrincipalUsuario.Criar(atual, Scheme));
            ctx.ShouldRenew = true;
        }
    }

    public static IServiceCollection AddPainelAuth(this IServiceCollection services)
    {
        services.AddAuthentication(Scheme)
            .AddCookie(Scheme, o =>
            {
                o.LoginPath = "/Login";
                o.LogoutPath = "/Logout";
                o.AccessDeniedPath = "/Login";
                o.Cookie.Name = "MaxiMassas.Painel";
                o.Cookie.HttpOnly = true;
                // Strict: o navegador nunca manda o cookie em requisições vindas de outro site.
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.SlidingExpiration = true;
                o.Events.OnValidatePrincipal = ValidarSessaoAsync;
            });

        services.AddAuthorization(o =>
            o.AddPolicy(Policy, p => p.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser()));

        return services;
    }
}

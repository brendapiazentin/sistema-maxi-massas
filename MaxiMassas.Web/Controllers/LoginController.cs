// Login do painel: confere usuário e senha (hash BCrypt) no servidor e cria o cookie de sessão.
using MaxiMassas.Web.Models;
using MaxiMassas.Web.Seguranca;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[AllowAnonymous]
[Route("Login")]
public class LoginController : Controller
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public LoginViewModel Vm { get; set; } = new();

    private readonly IAutenticacaoNegocio _autenticacao;

    private readonly ILogger<LoginController> _logger;

    public LoginController(IAutenticacaoNegocio autenticacao, ILogger<LoginController> logger)
    {
        _autenticacao = autenticacao;
        _logger = logger;
    }

    /// <summary>Só aceita voltar para páginas do próprio painel (evita redirecionar para outro site).</summary>
    private string Destino() => !string.IsNullOrEmpty(Vm.ReturnUrl) && Url.IsLocalUrl(Vm.ReturnUrl) ? Vm.ReturnUrl : "/Dashboard";

    [HttpGet("")]
    public IActionResult Index()
    {
        // Já tem sessão: não mostra o login de novo.
        if (User.Identity?.IsAuthenticated == true) return Redirect(Destino());
        return View("Index", Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        if (string.IsNullOrWhiteSpace(Vm.Usuario) || string.IsNullOrEmpty(Vm.Senha))
        {
            Vm.Erro = "Informe usuário e senha.";
            return View("Index", Vm);
        }

        var usuario = await _autenticacao.ConferirSenhaAsync(Vm.Usuario.Trim(), Vm.Senha);
        if (usuario is null)
        {
            Vm.Erro = "Usuário ou senha incorretos. Tente novamente.";
            Vm.Senha = string.Empty;
            return View("Index", Vm);
        }
        // Só avisa que está desativado depois de a senha conferir (não revela isso para quem chuta senhas).
        if (!usuario.Ativo)
        {
            Vm.Erro = "Este usuário está desativado. Fale com o administrador do sistema.";
            Vm.Senha = string.Empty;
            return View("Index", Vm);
        }

        await HttpContext.SignInAsync(PainelAuth.Scheme, PrincipalUsuario.Criar(usuario, PainelAuth.Scheme));
        _logger.LogInformation("[Painel] Login de {Login}", usuario.Login);
        return Redirect(Destino());
    }
}

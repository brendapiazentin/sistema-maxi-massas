// Esqueci minha senha (painel), passo 1: pede o e-mail ou login e manda o link por e-mail.
// A resposta é sempre a mesma, exista ou não a conta (regras em AuthNegocio).
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[AllowAnonymous]
[Route("EsqueciSenha")]
public class EsqueciSenhaController : Controller
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public EsqueciSenhaViewModel Vm { get; set; } = new();

    private readonly IAuthNegocio _auth;

    public EsqueciSenhaController(IAuthNegocio auth) => _auth = auth;

    [HttpGet("")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect("/Perfil"); // logado troca a senha no perfil
        return View("Index", Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        if (string.IsNullOrWhiteSpace(Vm.EmailOuLogin))
        {
            Vm.Erro = "Informe seu e-mail ou usuário.";
            return View("Index", Vm);
        }

        await _auth.SolicitarResetSenhaAsync(Vm.EmailOuLogin);
        Vm.Enviado = true;
        return View("Index", Vm);
    }
}

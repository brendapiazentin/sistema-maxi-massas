// Esqueci minha senha (painel), passo 2: a pessoa abre o link do e-mail (?token=...) e cria a nova senha.
// Link vale 1 hora e é de uso único; a nova senha segue a PoliticaSenha (regras em AuthNegocio).
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[AllowAnonymous]
[Route("RedefinirSenha")]
public class RedefinirSenhaController : Controller
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public RedefinirSenhaViewModel Vm { get; set; } = new();

    private readonly IAuthNegocio _auth;

    public RedefinirSenhaController(IAuthNegocio auth) => _auth = auth;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        // O token vai na URL; não deixa a página "vazar" o endereço para outros sites.
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Vm.LinkValido = await _auth.TokenValidoAsync(Vm.Token);
    return View(Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        try
        {
            await _auth.RedefinirSenhaAsync(new ResetPasswordRequest
            {
                Token = Vm.Token ?? string.Empty,
                NovaSenha = Vm.NovaSenha ?? string.Empty,
                ConfirmarSenha = Vm.ConfirmarSenha ?? string.Empty
            });
        }
        catch (InvalidOperationException ex)
        {
            Vm.Erro = ex.Message;
            Vm.LinkValido = await _auth.TokenValidoAsync(Vm.Token ?? string.Empty);
            return View("Index", Vm);
        }

        TempData["Aviso"] = "Senha alterada! Entre com a nova senha.";
        return RedirectToAction("Index", "Login");
    }
}

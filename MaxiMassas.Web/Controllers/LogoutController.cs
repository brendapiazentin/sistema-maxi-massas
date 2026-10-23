// Sair do painel: apaga o cookie de sessão. Só por POST (o botão "Sair" do menu envia um formulário
// com token antifalsificação), para um link de outro site não conseguir deslogar ninguém.
using MaxiMassas.Web.Seguranca;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("Logout")]
public class LogoutController : PainelController
{
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        await HttpContext.SignOutAsync(PainelAuth.Scheme);
        return RedirectToAction("Index", "Login");
    }
}

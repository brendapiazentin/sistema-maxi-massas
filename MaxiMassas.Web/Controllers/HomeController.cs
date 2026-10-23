using System.Diagnostics;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

/// <summary>Raiz do site: o painel abre no Dashboard (ou no login, se não houver sessão).</summary>
public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");

    [AllowAnonymous]
    [HttpGet("/Erro")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

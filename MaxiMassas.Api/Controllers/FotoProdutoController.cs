// Foto de um produto para o app: GET /api/app/produtos/{id}/foto (anonimo, como o cardapio).
// O app recebe o endereco pronto em "fotoUrl" (com ?v=versao), entao o cache pode ser longo:
// quando a foto muda, a versao e o endereco mudam.
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Api.Controllers;

[ApiController]
[Route("api/app/produtos/{id:int}/foto")]
[AllowAnonymous]
public class FotoProdutoController : ControllerBase
{
    private readonly IFotoProdutoService _fotos;

    public FotoProdutoController(IFotoProdutoService fotos) { _fotos = fotos; }

    [HttpGet]
    public async Task<IActionResult> Obter(int id)
    {
        var foto = await _fotos.ObterAsync(id);
        if (foto is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(foto, "image/jpeg");
    }
}

// Foto de um produto no painel (/FotoProduto/{id}?v=versao). So para quem esta logado.
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("FotoProduto")]
public class FotoProdutoController : PainelController
{
    private readonly IFotoProdutoService _fotos;

    public FotoProdutoController(IFotoProdutoService fotos) { _fotos = fotos; }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var foto = await _fotos.ObterAsync(id);
        if (foto is null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(foto, "image/jpeg");
    }
}

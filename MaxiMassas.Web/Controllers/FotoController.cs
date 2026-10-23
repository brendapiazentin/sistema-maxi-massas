// Foto de perfil de um usuário do painel (/Foto/{id}?v=versão). Só para quem está logado.
// A URL leva a versão da foto, então o navegador pode guardar a imagem por muito tempo:
// quando a foto muda, muda a URL.
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("Foto")]
public class FotoController : PainelController
{
    private readonly IUsuarioNegocio _usuarios;

    public FotoController(IUsuarioNegocio usuarios) { _usuarios = usuarios; }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Index(int id)
    {
        var foto = await _usuarios.ObterFotoAsync(id);
        if (foto is null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        return File(foto.Value.Foto, "image/jpeg");
    }
}

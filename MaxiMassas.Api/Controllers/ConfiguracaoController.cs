// ConfiguracaoController: parâmetros do negócio editados pelos gestores no sistema web.
//
//   GET /api/Configuracao/entrega   [usuario]        taxa de entrega e pedido mínimo para entrega grátis
//   PUT /api/Configuracao/entrega   [Administrador]  altera os dois valores
using MaxiMassas.Modelos.Dtos.Common;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Api.Controllers;

[ApiController][Route("api/[controller]")][Authorize][Produces("application/json")]
public class ConfiguracaoController : ControllerBase
{
    private readonly IConfiguracaoEntregaNegocio _svc;
    public ConfiguracaoController(IConfiguracaoEntregaNegocio svc) { _svc = svc; }

    [HttpGet("entrega")]
    public async Task<IActionResult> ObterEntrega()
        => Ok(ApiResponse<ConfiguracaoEntregaResponse>.Ok(await _svc.ObterResponseAsync()));

    [HttpPut("entrega")][Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AtualizarEntrega([FromBody] ConfiguracaoEntregaRequest req)
        => Ok(ApiResponse<ConfiguracaoEntregaResponse>.Ok(await _svc.AtualizarAsync(req), "Taxa de entrega atualizada."));
}

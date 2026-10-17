using MaxiMassas.Modelos.Dtos.Common;
using MaxiMassas.Negocio.Integracoes;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Api.Controllers;

[ApiController][Route("api/[controller]")][Authorize][Produces("application/json")]
public class ViaCepController : ControllerBase
{
    private readonly IClienteNegocio _svc;
    public ViaCepController(IClienteNegocio svc) { _svc = svc; }

    /// <summary>Consulta endereco completo a partir de um CEP via API ViaCEP.</summary>
    [HttpGet("{cep}")]
    public async Task<IActionResult> BuscarCep(string cep)
    {
        var endereco = await _svc.BuscarEnderecoPorCepAsync(cep);
        return endereco is null
            ? NotFound(ApiResponse.Fail($"CEP '{cep}' nao encontrado ou invalido."))
            : Ok(ApiResponse<EnderecoDto>.Ok(endereco, "Endereco encontrado com sucesso."));
    }
}
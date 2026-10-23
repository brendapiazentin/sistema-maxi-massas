// Consulta de CEP para o formulário de clientes (clientes.js). Usa o mesmo IClienteNegocio da API.
using MaxiMassas.Modelos.Dtos.Common;
using MaxiMassas.Negocio;
using MaxiMassas.Negocio.Integracoes;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("api/ViaCep")]
public class ViaCepController : PainelController
{
    private readonly IClienteNegocio _clientes;

    public ViaCepController(IClienteNegocio clientes) { _clientes = clientes; }

    /// <summary>Consulta o endereço completo a partir de um CEP, via ViaCEP.</summary>
    [HttpGet("{cep}")]
    public async Task<IActionResult> BuscarCep(string cep)
    {
        var endereco = await _clientes.BuscarEnderecoPorCepAsync(cep);
        return endereco is null
            ? NotFound(ApiResponse.Fail($"CEP '{cep}' nao encontrado ou invalido."))
            : Ok(ApiResponse<EnderecoDto>.Ok(endereco, "Endereco encontrado com sucesso."));
    }
}

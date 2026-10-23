// RF04 — Controller de Consumo Próprio
// Registra uso interno de produtos pelos proprietários da MaxiMassas.
// NÃO gera venda nem movimentação financeira — apenas histórico e baixa de estoque.
using MaxiMassas.Modelos.Dtos.Common;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Api.Controllers;

/// <summary>
/// Gerencia o consumo interno de produtos pelos proprietários.
/// Apenas Administradores podem registrar consumos próprios.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
[Produces("application/json")]
public class ConsumoProprioController : ControllerBase
{
    private readonly IConsumoProprioNegocio _service;
    private readonly IUsuarioNegocio _usuarios;

    public ConsumoProprioController(IConsumoProprioNegocio service, IUsuarioNegocio usuarios)
    {
        _service = service;
        _usuarios = usuarios;
    }

    // -------------------------------------------------------------------------
    // GET /api/consumoproprio
    // Retorna todo o histórico de consumos internos (do mais recente ao mais antigo)
    // -------------------------------------------------------------------------
    /// <summary>Lista todo o histórico de consumo próprio.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ConsumoProprioResponse>>.Ok(resultado,
            "Histórico de consumo próprio carregado com sucesso."));
    }

    // -------------------------------------------------------------------------
    // GET /api/consumoproprio/periodo?inicio=2024-01-01&fim=2024-12-31
    // Filtra por intervalo de datas (útil para relatórios)
    // -------------------------------------------------------------------------
    /// <summary>Lista consumos filtrados por período.</summary>
    [HttpGet("periodo")]
    public async Task<IActionResult> GetByPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fim)
    {
        if (inicio > fim)
            return BadRequest(ApiResponse.Fail("A data de início deve ser anterior à data de fim."));

        var resultado = await _service.GetByPeriodoAsync(inicio, fim);
        return Ok(ApiResponse<IEnumerable<ConsumoProprioResponse>>.Ok(resultado));
    }

    // -------------------------------------------------------------------------
    // GET /api/consumoproprio/produto/{produtoId}
    // Histórico de consumo de um produto específico
    // -------------------------------------------------------------------------
    /// <summary>Lista consumos de um produto específico.</summary>
    [HttpGet("produto/{produtoId:int}")]
    public async Task<IActionResult> GetByProduto(int produtoId)
    {
        var resultado = await _service.GetByProdutoIdAsync(produtoId);
        return Ok(ApiResponse<IEnumerable<ConsumoProprioResponse>>.Ok(resultado));
    }

    // -------------------------------------------------------------------------
    // POST /api/consumoproprio
    // Registra novo consumo interno.
    // O usuário autenticado é capturado automaticamente (não é informado no body).
    // -------------------------------------------------------------------------
    /// <summary>
    /// Registra consumo interno de produto pelo proprietário.
    /// Reduz o estoque sem gerar venda ou movimentação financeira.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] ConsumoProprioRequest request)
    {
        // Identifica o usuário autenticado via Basic Auth para associar ao consumo
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized(ApiResponse.Fail("Usuário não autenticado."));

        var usuario = await _usuarios.GetByIdAsync(usuarioId);
        if (usuario is null)
            return Unauthorized(ApiResponse.Fail("Usuário não encontrado."));

        var resultado = await _service.RegistrarConsumoAsync(request, usuarioId);

        // Preenche o nome do usuário no response (não disponível no service)
        resultado.NomeUsuario = usuario.Nome;

        return Created($"api/consumoproprio/{resultado.Id}",
            ApiResponse<ConsumoProprioResponse>.Ok(resultado,
                $"Consumo próprio de {resultado.Quantidade}x {resultado.NomeProduto} registrado. Estoque restante: {resultado.EstoqueRestante}."));
    }
}

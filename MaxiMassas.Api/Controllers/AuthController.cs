// AuthController: Autenticação + US16 Redefinição de Senha
using MaxiMassas.Modelos.Dtos.Common;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioNegocio _usuarios;
    private readonly IAuthNegocio _authService;

    public AuthController(IUsuarioNegocio usuarios, IAuthNegocio authService)
    {
        _usuarios = usuarios;
        _authService = authService;
    }

    /// <summary>Retorna os dados do usuario autenticado via Basic Auth.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Unauthorized(ApiResponse.Fail("Nao autenticado."));
        var usuario = await _usuarios.GetByIdAsync(id);
        if (usuario is null) return Unauthorized(ApiResponse.Fail("Usuario nao encontrado."));
        return Ok(ApiResponse<UsuarioResponse>.Ok(usuario, $"Bem-vindo, {usuario.Nome}!"));
    }

    // -------------------------------------------------------------------------
    // US16 — Esqueci minha senha (painel). As páginas Razor EsqueciSenha/RedefinirSenha usam o mesmo
    // IAuthNegocio; estes endpoints existem para integrações. Regras completas em AuthNegocio.
    // SEGURANÇA: o passo 1 sempre responde igual, exista ou não a conta.
    // -------------------------------------------------------------------------
    /// <summary>Envia o link de redefinição (válido por 1 hora, uso único) para o e-mail do usuário. Aceita e-mail ou login.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse.Fail("Informe o e-mail ou o login."));

        await _authService.SolicitarResetSenhaAsync(request.Email);
        return Ok(ApiResponse.Ok(
            "Se houver um usuário ativo com e-mail cadastrado, enviamos um link para criar uma nova senha."));
    }

    /// <summary>Cria a nova senha com o token do link. O token é invalidado após o uso.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // InvalidOperationException (link expirado, senha fraca...) vira 400 no ErrorHandlingMiddleware
        await _authService.RedefinirSenhaAsync(request);
        return Ok(ApiResponse.Ok("Senha redefinida com sucesso. Você já pode entrar com a nova senha."));
    }
}

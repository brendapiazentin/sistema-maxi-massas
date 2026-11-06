// Middleware global de tratamento de erros. Captura excecoes e retorna JSON padronizado.
// Também registra no log (e, pelo LogBancoProvider, na tabela LogsSistema) o que deu errado:
// metodo, rota, quem chamou e o resultado. Erro de regra (4xx) = Aviso; erro interno (500) = Erro.
// Nunca registra query string nem corpo da requisicao (podem conter senha/token).
using MaxiMassas.Modelos.Dtos.Common;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace MaxiMassas.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (KeyNotFoundException ex) { await Handle(context, ex, HttpStatusCode.NotFound, ex.Message); }
        catch (InvalidOperationException ex) { await Handle(context, ex, HttpStatusCode.BadRequest, ex.Message); }
        catch (MaxiMassas.Negocio.RegraDeNegocioException ex) { await Handle(context, ex, HttpStatusCode.BadRequest, ex.Message); }
        catch (UnauthorizedAccessException ex) { await Handle(context, ex, HttpStatusCode.Forbidden, "Acesso negado."); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[API] Erro interno em {Metodo} {Rota} ({Quem}): {Message}",
                context.Request.Method, context.Request.Path.Value, Quem(context), ex.Message);
            await Handle(context, ex, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno. Tente novamente.", registrar: false);
        }
    }

    private async Task Handle(HttpContext ctx, Exception ex, HttpStatusCode code, string msg, bool registrar = true)
    {
        if (registrar)
            _logger.LogWarning("[API] {Metodo} {Rota} ({Quem}) -> {Status}: {Mensagem}",
                ctx.Request.Method, ctx.Request.Path.Value, Quem(ctx), (int)code, msg);

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)code;
        // Só a mensagem vai para o cliente: o nome da exceção é detalhe interno e aparecia no app.
        var resp = ApiResponse.Fail(msg);
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(resp,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    // Ex.: "Cliente 12" (app) ou "Administrador 1" (painel). Só papel e id, nunca e-mail ou senha.
    private static string Quem(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated != true) return "anonimo";
        var papel = ctx.User.FindFirst(ClaimTypes.Role)?.Value ?? "?";
        var id = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "?";
        return $"{papel} {id}";
    }
}

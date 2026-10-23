// Estoque do painel: lista montada no servidor, mais as duas ações da tela, gravadas pelo EstoqueNegocio:
//   - Ajustar: define a quantidade exata (contagem) e registra um Ajuste "de X para Y";
//   - Reposição: soma as quantidades informadas, com uma Entrada para cada produto.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("Estoque")]
public class EstoqueController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public EstoqueViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.PodeAjustar = User.IsInRole(nameof(TipoUsuario.Administrador));
        base.OnActionExecuting(context);
    }

    private readonly IProdutoNegocio _produtos;

    private readonly IEstoqueNegocio _estoque;

    private readonly ILogger<EstoqueController> _logger;

    public EstoqueController(IProdutoNegocio produtos, IEstoqueNegocio estoque, ILogger<EstoqueController> logger)
    {
        _produtos = produtos;
        _estoque = estoque;
        _logger = logger;
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        Vm.Erro = mensagem;
        await CarregarAsync();
        return View("Index", Vm);
    }

    private async Task CarregarAsync()
    {
        Vm.Produtos = (await _produtos.GetAllAsync()).OrderBy(p => p.Nome).ThenBy(p => p.Sabor).ToList();
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost("Ajuste")]
    public async Task<IActionResult> Ajuste(int produtoId, int? quantidade)
    {
        if (!Vm.PodeAjustar) return await ComErroAsync("Somente administradores podem ajustar o estoque. Para dar entrada em mercadoria, use a Reposição.");
        var produto = await _produtos.GetByIdAsync(produtoId);
        if (produto is null) return await ComErroAsync("Este produto não existe mais.");
        if (quantidade is null or < 0) return await ComErroAsync("Informe o novo estoque (0 ou mais).");

        try
        {
            await _estoque.AjustarQuantidadeAsync(produtoId, quantidade.Value, "Ajuste manual no painel");
        }
        catch (InvalidOperationException ex)
        {
            return await ComErroAsync(ex.Message);
        }

        TempData["Mensagem"] = $"Estoque de {EstoqueViewModel.NomeCompleto(produto)} ajustado para {quantidade} un.";
        return RedirectToAction("Index");
    }

    [HttpPost("Reposicao")]
    public async Task<IActionResult> Reposicao(List<ItemReposicaoForm> itens)
    {
        var repor = itens.Where(i => i.Quantidade > 0).ToList();
        if (repor.Count == 0) return await ComErroAsync("Informe a quantidade de pelo menos um produto para repor.");

        var unidades = 0;
        foreach (var item in repor)
        {
            var estoque = await _estoque.GarantirEstoqueAsync(item.ProdutoId, PainelFormatos.AlertaMinimoPadrao);
            await _estoque.RegistrarMovimentacaoAsync(new MovimentacaoEstoqueRequest
            {
                EstoqueId = estoque.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = item.Quantidade,
                Motivo = "Reposição pelo painel"
            });
            unidades += item.Quantidade;
        }

        _logger.LogInformation("[Painel] Reposição de {N} produto(s), {U} unidades.", repor.Count, unidades);
        TempData["Mensagem"] = $"Reposição registrada: {unidades} unidade(s) em {repor.Count} produto(s).";
        return RedirectToAction("Index");
    }
}

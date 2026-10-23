// Vendas do painel: a lista (uma linha por item, como antes) é montada no servidor, e o modal de
// Consumo próprio grava pelo ConsumoProprioNegocio (baixa o estoque sem gerar venda).
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MaxiMassas.Web.Controllers;

[Route("Vendas")]
public class VendasController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public VendasViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.PodeRegistrarConsumo = User.IsInRole(nameof(TipoUsuario.Administrador));
        base.OnActionExecuting(context);
    }

    private readonly IVendaNegocio _vendas;

    private readonly IClienteNegocio _clientes;

    private readonly IProdutoNegocio _produtos;

    private readonly IConsumoProprioNegocio _consumo;

    private readonly ILogger<VendasController> _logger;

    public VendasController(IVendaNegocio vendas, IClienteNegocio clientes, IProdutoNegocio produtos,
        IConsumoProprioNegocio consumo, ILogger<VendasController> logger)
    {
        _vendas = vendas;
        _clientes = clientes;
        _produtos = produtos;
        _consumo = consumo;
        _logger = logger;
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        Vm.ErroConsumo = mensagem;
        await CarregarAsync();
        return View("Index", Vm);
    }

    private async Task CarregarAsync()
    {
        Vm.Vendas = (await _vendas.GetAllAsync()).OrderByDescending(v => v.Data).ToList();
        Vm.TelefonePorCliente = (await _clientes.GetAllAsync()).ToDictionary(c => c.Id, c => c.Telefone);
        Vm.Total = Vm.Vendas.Where(v => v.Status != StatusVenda.Cancelada).Sum(v => v.ValorTotal);
        Vm.Anos = Vm.Vendas.Select(v => v.Data.Year).Append(DateTime.Today.Year).Distinct().OrderByDescending(a => a).ToList();

        var produtos = (await _produtos.GetAllAsync()).OrderBy(p => p.Nome).ToList();
        Vm.NomesProdutos = produtos.Select(p => p.Nome).Distinct().ToList();
        Vm.ProdutosConsumoJson = JsonSerializer.Serialize(produtos.Select(p => new
        {
            id = p.Id,
            nome = p.Nome,
            sabor = string.IsNullOrEmpty(p.Sabor) ? "Tradicional" : p.Sabor,
            peso = PainelFormatos.Peso(p.Peso),
            custo = PainelFormatos.Moeda(p.Custo),
            estoque = p.Estoque?.Quantidade ?? 0
        }));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost("Consumo")]
    public async Task<IActionResult> Consumo(int produtoId, int? quantidade)
    {
        if (!Vm.PodeRegistrarConsumo) return await ComErroAsync("Somente administradores podem registrar consumo próprio.");
        if (produtoId <= 0) return await ComErroAsync("Selecione o produto na busca.");
        if (quantidade is null or <= 0) return await ComErroAsync("Informe a quantidade (1 ou mais).");

        try
        {
            var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var registro = await _consumo.RegistrarConsumoAsync(
                new ConsumoProprioRequest { ProdutoId = produtoId, Quantidade = quantidade.Value, Observacao = "Registrado no painel" },
                usuarioId);
            _logger.LogInformation("[Painel] Consumo próprio de {Qtd} un. do produto {ProdutoId}.", quantidade, produtoId);
            TempData["Mensagem"] = $"Consumo próprio registrado: {quantidade} un. de {registro.NomeProduto}. Restam {registro.EstoqueRestante}.";
            return RedirectToAction("Index");
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
        {
            return await ComErroAsync(ex.Message);
        }
    }
}

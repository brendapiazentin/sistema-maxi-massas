// Dashboard: indicadores montados no servidor, pelos mesmos services dos relatórios da API.
// O período (mês/ano) vem na URL (?mes=10&ano=2026); trocar o filtro recarrega a página.
// Valores financeiros e rankings são só para Administrador (mesma regra da API /api/Relatorio);
// para os demais perfis aparecem como "—".
using MaxiMassas.Modelos.Dtos.Relatorio;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("Dashboard")]
public class DashboardController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public DashboardViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.VeFinanceiro = User.IsInRole(nameof(TipoUsuario.Administrador));
        base.OnActionExecuting(context);
    }

    private readonly IRelatorioNegocio _relatorios;

    private readonly IConsumoProprioNegocio _consumo;

    private readonly IEstoqueNegocio _estoque;

    private readonly IProdutoNegocio _produtos;

    private readonly IVendaNegocio _vendas;

    public DashboardController(IRelatorioNegocio relatorios, IConsumoProprioNegocio consumo, IEstoqueNegocio estoque,
        IProdutoNegocio produtos, IVendaNegocio vendas)
    {
        _relatorios = relatorios;
        _consumo = consumo;
        _estoque = estoque;
        _produtos = produtos;
        _vendas = vendas;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var hoje = DateTime.Today;
        Vm.Mes = Vm.Mes is >= 1 and <= 12 ? Vm.Mes : hoje.Month;
        Vm.Ano = Vm.Ano is >= 2000 and <= 2100 ? Vm.Ano : hoje.Year;
        Vm.Anos = Enumerable.Range(hoje.Year - 2, 3).Append(Vm.Ano.Value).Distinct().OrderBy(a => a).ToList();

        var inicioMes = new DateTime(Vm.Ano.Value, Vm.Mes.Value, 1);
        var fimMes = inicioMes.AddMonths(1).AddTicks(-1);
        var fimHoje = hoje.AddDays(1).AddTicks(-1);

        if (Vm.VeFinanceiro)
        {
            Vm.Hoje = await _relatorios.GetRelatorioFinanceiroAsync(hoje, fimHoje);
            Vm.Periodo = await _relatorios.GetRelatorioFinanceiroAsync(inicioMes, fimMes);
            Vm.ConsumidosNoPeriodo = (await _consumo.GetByPeriodoAsync(inicioMes, fimMes)).Sum(c => c.Quantidade);
            Vm.LucroHistorico = (await _relatorios.GetLucroPorPeriodoAsync(new DateTime(2000, 1, 1), fimMes)).LucroBruto;
            Vm.TopClientes = (await _relatorios.GetTopClientesAsync(inicioMes, fimMes, 3)).ToList();
            Vm.TopProdutos = (await _relatorios.GetProdutosMaisVendidosAsync(inicioMes, fimMes, 5)).ToList();
        }

        var nomes = (await _produtos.GetAllAsync())
            .ToDictionary(p => p.Id, p => string.IsNullOrEmpty(p.Sabor) ? p.Nome : $"{p.Nome} {p.Sabor}");
        Vm.EstoqueBaixo = (await _estoque.GetEstoquesBaixosAsync())
            .OrderBy(e => e.Quantidade)
            .Select(e => (nomes.GetValueOrDefault(e.ProdutoId, $"Produto #{e.ProdutoId}"), e.Quantidade))
            .ToList();
        Vm.EntregasHoje = (await _vendas.GetAllAsync())
            .Where(v => v.Entrega is not null && v.Entrega.DataEntrega.Date == hoje && v.Status != StatusVenda.Cancelada)
            .OrderBy(v => v.Entrega!.Horario)
            .ToList();
    return View(Vm);
    }
}

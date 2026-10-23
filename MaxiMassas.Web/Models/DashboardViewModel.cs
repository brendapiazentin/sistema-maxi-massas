// ViewModel da tela "Dashboard": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Relatorio;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

public class DashboardViewModel
{
    public int? Mes { get; set; }

    public int? Ano { get; set; }

    [BindNever] public bool VeFinanceiro { get; set; }

    [BindNever] public RelatorioFinanceiroDto? Hoje { get; set; }

    [BindNever] public RelatorioFinanceiroDto? Periodo { get; set; }

    [BindNever] public int? ConsumidosNoPeriodo { get; set; }

    [BindNever] public decimal? LucroHistorico { get; set; }

    [BindNever] public IReadOnlyList<(string Nome, int Quantidade)> EstoqueBaixo { get; set; } = Array.Empty<(string, int)>();

    [BindNever] public IReadOnlyList<VendaResponse> EntregasHoje { get; set; } = Array.Empty<VendaResponse>();

    [BindNever] public IReadOnlyList<TopClienteDto> TopClientes { get; set; } = Array.Empty<TopClienteDto>();

    [BindNever] public IReadOnlyList<ProdutoMaisVendidoDto> TopProdutos { get; set; } = Array.Empty<ProdutoMaisVendidoDto>();

    [BindNever] public IReadOnlyList<int> Anos { get; set; } = Array.Empty<int>();

    public static string Margem(RelatorioFinanceiroDto? r) =>
        r is null ? "—" : r.TotalFaturado > 0 ? $"{r.LucroBruto / r.TotalFaturado * 100:0}%" : "0%";

    public static string Dinheiro(decimal? valor) => valor is null ? "—" : PainelFormatos.Moeda(valor.Value);
}

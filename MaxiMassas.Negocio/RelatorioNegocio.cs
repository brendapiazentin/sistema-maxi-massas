// RelatorioNegocio — atualizado para usar Venda.Status (UML) em vez de Venda.StatusPagamento.
// StatusPagamento agora pertence à entidade Pagamento.
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos.Dtos.Relatorio;
using MaxiMassas.Modelos;

namespace MaxiMassas.Negocio;

public class RelatorioNegocio : IRelatorioNegocio
{
    private readonly IRelatorioRepositorio _repo;

    public RelatorioNegocio(IRelatorioRepositorio repo) { _repo = repo; }

    // Helper: normaliza período para início e fim do dia
    private static (DateTime inicio, DateTime fim) NormalizarPeriodo(DateTime inicio, DateTime fim)
        => (inicio.Date, fim.Date.AddDays(1));

    public async Task<FaturamentoPorPeriodoDto> GetFaturamentoPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        // UML: usa Venda.Status para filtrar canceladas (StatusVenda.Cancelada)
        var vendas = await _repo.GetVendasNaoCanceladasAsync(ini, fimD);

        return new FaturamentoPorPeriodoDto
        {
            DataInicio = ini, DataFim = fim,
            TotalFaturado = vendas.Sum(v => v.ValorTotal),
            TotalVendas = vendas.Count
        };
    }

    public async Task<LucroPorPeriodoDto> GetLucroPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        var itens = await _repo.GetItensNaoCanceladosAsync(ini, fimD);

        var faturamento = itens.Sum(i => i.Subtotal);
        var custo = itens.Sum(i => i.Produto.Custo * i.Quantidade);
        var lucro = faturamento - custo;

        return new LucroPorPeriodoDto
        {
            DataInicio = ini, DataFim = fim,
            TotalFaturado = faturamento, TotalCusto = custo, LucroBruto = lucro,
            MargemLucro = faturamento > 0 ? Math.Round(lucro / faturamento * 100, 2) : 0
        };
    }

    public async Task<decimal> GetTicketMedioAsync(DateTime inicio, DateTime fim)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        // Ticket médio apenas das vendas concluídas (pagamento confirmado)
        var vendas = await _repo.GetVendasPagasAsync(ini, fimD);

        return vendas.Count > 0 ? vendas.Average(x => x.ValorTotal) : 0;
    }

    public async Task<IEnumerable<ProdutoMaisVendidoDto>> GetProdutosMaisVendidosAsync(DateTime inicio, DateTime fim, int top = 10)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        return await _repo.GetProdutosMaisVendidosAsync(ini, fimD, top);
    }

    public async Task<IEnumerable<HistoricoVendaDto>> GetHistoricoVendasAsync(DateTime inicio, DateTime fim)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        return await _repo.GetHistoricoVendasAsync(ini, fimD);
    }

    public async Task<RelatorioFinanceiroDto> GetRelatorioFinanceiroAsync(DateTime inicio, DateTime fim)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        var vendas = await _repo.GetVendasComItensAsync(ini, fimD);

        var ativas = vendas.Where(v => v.Status != StatusVenda.Cancelada).ToList();
        var faturamento = ativas.Sum(v => v.ValorTotal);
        var custo = ativas.SelectMany(v => v.ItensVenda).Sum(i => i.Produto.Custo * i.Quantidade);

        return new RelatorioFinanceiroDto
        {
            TotalFaturado = faturamento,
            TotalCusto = custo,
            LucroBruto = faturamento - custo,
            TicketMedio = ativas.Count > 0 ? ativas.Average(v => v.ValorTotal) : 0,
            TotalVendas = vendas.Count,
            // Usa StatusPagamento do Pagamento (UML) para contar pagas
            VendasPagas = vendas.Count(v => v.Pagamento?.StatusPagamento == StatusPagamento.Pago),
            VendasPendentes = vendas.Count(v => v.Pagamento?.StatusPagamento == StatusPagamento.Pendente),
            VendasCanceladas = vendas.Count(v => v.Status == StatusVenda.Cancelada)
        };
    }

    // RF08 / US15 — Top N Clientes por Valor Gasto
    public async Task<IEnumerable<TopClienteDto>> GetTopClientesAsync(DateTime inicio, DateTime fim, int top = 5)
    {
        var (ini, fimD) = NormalizarPeriodo(inicio, fim);

        var ranking = await _repo.GetTopClientesAsync(ini, fimD, top);

        for (int i = 0; i < ranking.Count; i++)
            ranking[i].Posicao = i + 1;

        return ranking;
    }
}

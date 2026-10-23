using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Dtos.Relatorio;

namespace MaxiMassas.Repositorio.Interfaces;

/// <summary>
/// Consultas dos relatórios gerenciais. Todas recebem o período já normalizado:
/// início do primeiro dia (inclusive) e início do dia seguinte ao último (exclusivo).
/// A camada de Negócio só calcula indicadores em cima do que volta daqui.
/// </summary>
public interface IRelatorioRepositorio
{
    /// <summary>Vendas do período que não foram canceladas.</summary>
    Task<List<Venda>> GetVendasNaoCanceladasAsync(DateTime inicio, DateTime fimExclusivo);

    /// <summary>Itens (com venda e produto) das vendas não canceladas do período.</summary>
    Task<List<ItemVenda>> GetItensNaoCanceladosAsync(DateTime inicio, DateTime fimExclusivo);

    /// <summary>Vendas do período com pagamento confirmado (Pago).</summary>
    Task<List<Venda>> GetVendasPagasAsync(DateTime inicio, DateTime fimExclusivo);

    /// <summary>Todas as vendas do período (inclusive canceladas) com itens, produtos e pagamento.</summary>
    Task<List<Venda>> GetVendasComItensAsync(DateTime inicio, DateTime fimExclusivo);

    Task<List<ProdutoMaisVendidoDto>> GetProdutosMaisVendidosAsync(DateTime inicio, DateTime fimExclusivo, int top);

    Task<List<HistoricoVendaDto>> GetHistoricoVendasAsync(DateTime inicio, DateTime fimExclusivo);

    /// <summary>Ranking por valor gasto, sem a posição (quem numera é a camada de Negócio).</summary>
    Task<List<TopClienteDto>> GetTopClientesAsync(DateTime inicio, DateTime fimExclusivo, int top);
}

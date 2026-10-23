using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Dtos.Relatorio;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class RelatorioRepositorio : IRelatorioRepositorio
{
    private readonly AppDbContext _context;

    public RelatorioRepositorio(AppDbContext context) => _context = context;

    public Task<List<Venda>> GetVendasNaoCanceladasAsync(DateTime inicio, DateTime fimExclusivo)
        => _context.Vendas.AsNoTracking()
            .Where(v => v.Data >= inicio && v.Data < fimExclusivo && v.Status != StatusVenda.Cancelada)
            .ToListAsync();

    public Task<List<ItemVenda>> GetItensNaoCanceladosAsync(DateTime inicio, DateTime fimExclusivo)
        => _context.ItensVenda.AsNoTracking()
            .Include(i => i.Venda).Include(i => i.Produto)
            .Where(i => i.Venda.Data >= inicio && i.Venda.Data < fimExclusivo
                     && i.Venda.Status != StatusVenda.Cancelada)
            .ToListAsync();

    public Task<List<Venda>> GetVendasPagasAsync(DateTime inicio, DateTime fimExclusivo)
        => _context.Vendas.AsNoTracking()
            .Include(v => v.Pagamento)
            .Where(v => v.Data >= inicio && v.Data < fimExclusivo
                     && v.Pagamento != null
                     && v.Pagamento.StatusPagamento == StatusPagamento.Pago)
            .ToListAsync();

    public Task<List<Venda>> GetVendasComItensAsync(DateTime inicio, DateTime fimExclusivo)
        => _context.Vendas.AsNoTracking()
            .Include(v => v.ItensVenda).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamento)
            .Where(v => v.Data >= inicio && v.Data < fimExclusivo)
            .ToListAsync();

    public Task<List<ProdutoMaisVendidoDto>> GetProdutosMaisVendidosAsync(DateTime inicio, DateTime fimExclusivo, int top)
        => _context.ItensVenda.AsNoTracking()
            .Where(i => i.Venda.Data >= inicio && i.Venda.Data < fimExclusivo
                     && i.Venda.Status != StatusVenda.Cancelada)
            .GroupBy(i => new { i.ProdutoId, i.Produto.Nome, i.Produto.Sabor })
            .Select(g => new ProdutoMaisVendidoDto
            {
                ProdutoId = g.Key.ProdutoId,
                // Nome + sabor ("Pizza Calabresa"), montado no próprio SQL
                NomeProduto = g.Key.Sabor == null || g.Key.Sabor == "" ? g.Key.Nome : g.Key.Nome + " " + g.Key.Sabor,
                QuantidadeVendida = g.Sum(i => i.Quantidade),
                TotalFaturado = g.Sum(i => i.Subtotal)
            })
            .OrderByDescending(p => p.QuantidadeVendida)
            .Take(top)
            .ToListAsync();

    public Task<List<HistoricoVendaDto>> GetHistoricoVendasAsync(DateTime inicio, DateTime fimExclusivo)
        => _context.Vendas.AsNoTracking()
            .Where(v => v.Data >= inicio && v.Data < fimExclusivo)
            .OrderByDescending(v => v.Data)
            .Select(v => new HistoricoVendaDto
            {
                VendaId = v.Id,
                Data = v.Data,
                NomeCliente = v.Cliente.Nome,
                ValorTotal = v.ValorTotal,
                StatusPagamento = v.Status.ToString(),
                Origem = v.Origem
            })
            .ToListAsync();

    public Task<List<TopClienteDto>> GetTopClientesAsync(DateTime inicio, DateTime fimExclusivo, int top)
        => _context.Vendas.AsNoTracking()
            .Where(v => v.Data >= inicio && v.Data < fimExclusivo && v.Status != StatusVenda.Cancelada)
            .GroupBy(v => new { v.ClienteId, v.Cliente.Nome })
            .Select(g => new TopClienteDto
            {
                ClienteId = g.Key.ClienteId,
                NomeCliente = g.Key.Nome,
                TotalGasto = g.Sum(v => v.ValorTotal),
                QuantidadeCompras = g.Count()
            })
            .OrderByDescending(c => c.TotalGasto)
            .Take(top)
            .ToListAsync();
}

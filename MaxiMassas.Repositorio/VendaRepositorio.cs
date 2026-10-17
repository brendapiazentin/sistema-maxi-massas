using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class VendaRepositorio : Repositorio<Venda>, IVendaRepositorio
{
    public VendaRepositorio(AppDbContext context) : base(context) { }

    public async Task<Venda?> GetVendaComDetalhesAsync(int id)
        => await _context.Vendas
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .Include(v => v.ItensVenda).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamento).Include(v => v.Entrega)
            .AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<Venda>> GetVendasComDetalhesAsync()
        => await _context.Vendas
            .Include(v => v.Cliente).Include(v => v.Usuario)
            .Include(v => v.ItensVenda).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamento).Include(v => v.Entrega)
            .AsNoTracking().OrderByDescending(v => v.Data).ToListAsync();

    public async Task<IEnumerable<Venda>> GetVendasDoClienteAsync(int clienteId)
        => await _context.Vendas
            .Include(v => v.ItensVenda).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamento).Include(v => v.Entrega)
            .AsNoTracking().Where(v => v.ClienteId == clienteId)
            .OrderByDescending(v => v.Data).ToListAsync();

    public async Task<IEnumerable<Venda>> GetVendasPorPeriodoAsync(DateTime inicio, DateTime fim)
        => await _context.Vendas
            .Include(v => v.Cliente).Include(v => v.ItensVenda).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamento).AsNoTracking()
            .Where(v => v.Data >= inicio && v.Data <= fim).OrderByDescending(v => v.Data).ToListAsync();
}
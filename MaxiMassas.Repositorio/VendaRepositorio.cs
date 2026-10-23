using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Dtos.Response;
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

    /// <summary>Mesmos status que a tela de Vendas mostra como "Pendente".</summary>
    private static readonly StatusVenda[] StatusPendentes = { StatusVenda.Nenhum, StatusVenda.Aberta, StatusVenda.EmProcessamento };

    public async Task<PedidosAppPendentesResponse> GetPedidosAppPendentesAsync(string origem, int limite)
    {
        var pendentes = _context.Vendas.AsNoTracking()
            .Where(v => v.Origem == origem && StatusPendentes.Contains(v.Status));

        var pedidos = await pendentes
            .OrderByDescending(v => v.Id)
            .Take(limite)
            .Select(v => new PedidoAppPendenteResponse
            {
                Id = v.Id,
                Cliente = v.Cliente.Nome,
                ValorTotal = v.ValorTotal,
                Data = v.Data,
                Entrega = v.Entrega != null && v.Entrega.TipoEntrega == TipoEntrega.Entrega
            })
            .ToListAsync();

        var total = pedidos.Count < limite ? pedidos.Count : await pendentes.CountAsync();
        return new PedidosAppPendentesResponse { Total = total, Pedidos = pedidos };
    }
}

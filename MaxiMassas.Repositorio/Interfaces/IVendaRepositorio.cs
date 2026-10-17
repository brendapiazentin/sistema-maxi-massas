using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Dtos.Response;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IVendaRepositorio : IRepositorio<Venda>
{
    Task<Venda?> GetVendaComDetalhesAsync(int id);
    Task<IEnumerable<Venda>> GetVendasPorPeriodoAsync(DateTime inicio, DateTime fim);
    Task<IEnumerable<Venda>> GetVendasComDetalhesAsync();
    Task<IEnumerable<Venda>> GetVendasDoClienteAsync(int clienteId);

    /// <summary>
    /// Pedidos feitos pelo app (<paramref name="origem"/>) que ainda estão "Pendentes" (ninguém mudou o status),
    /// os <paramref name="limite"/> mais recentes, mais o total de pendentes.
    /// </summary>
    Task<PedidosAppPendentesResponse> GetPedidosAppPendentesAsync(string origem, int limite);
}

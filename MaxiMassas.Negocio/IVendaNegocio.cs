// Contrato do VendaNegocio — alinhado ao diagrama UML.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;

namespace MaxiMassas.Negocio;

public interface IVendaNegocio
{
    Task<IEnumerable<VendaResponse>> GetAllAsync();
    Task<VendaResponse?> GetByIdAsync(int id);
    /// <summary>Cria venda, reduz estoque e registra movimentações. UML regras 8 e 9.</summary>
    Task<VendaResponse> CreateAsync(VendaRequest request, int? usuarioId);
    /// <summary>Atualiza Status da Venda e StatusPagamento do Pagamento de forma independente.</summary>
    Task<VendaResponse?> UpdateAsync(int id, VendaUpdateRequest request);
    Task<bool> DeleteAsync(int id);
    /// <summary>Pedidos do app ainda pendentes (aviso de pedido novo do painel).</summary>
    Task<PedidosAppPendentesResponse> GetPedidosAppPendentesAsync();
}

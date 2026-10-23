using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;

namespace MaxiMassas.Negocio;

public interface IVariacaoProdutoNegocio
{
    Task<IEnumerable<VariacaoProdutoResponse>> GetByProdutoIdAsync(int produtoId);
    Task<VariacaoProdutoResponse> CreateAsync(VariacaoProdutoRequest request);
    Task<bool> DeleteAsync(int id);
}
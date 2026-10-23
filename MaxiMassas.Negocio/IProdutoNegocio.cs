using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;

namespace MaxiMassas.Negocio;

public interface IProdutoNegocio
{
    Task<IEnumerable<ProdutoResponse>> GetAllAsync();
    Task<ProdutoResponse?> GetByIdAsync(int id);
    Task<ProdutoResponse> CreateAsync(ProdutoRequest request);
    Task<ProdutoResponse?> UpdateAsync(int id, ProdutoUpdateRequest request);
    Task<bool> DeleteAsync(int id);
}
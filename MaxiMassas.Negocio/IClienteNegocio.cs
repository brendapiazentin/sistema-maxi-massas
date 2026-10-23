using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio.Integracoes;

namespace MaxiMassas.Negocio;

public interface IClienteNegocio
{
    Task<IEnumerable<ClienteResponse>> GetAllAsync();
    Task<ClienteResponse?> GetByIdAsync(int id);
    Task<ClienteResponse> CreateAsync(ClienteRequest request);
    Task<ClienteResponse?> UpdateAsync(int id, ClienteUpdateRequest request);
    Task<bool> DeleteAsync(int id);
    Task<EnderecoDto?> BuscarEnderecoPorCepAsync(string cep);
}
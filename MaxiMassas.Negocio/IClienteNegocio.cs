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
    /// <summary>Clientes cujo nome contém o trecho informado (executa a stored procedure sp_Clientes_BuscarPorNome).</summary>
    Task<IEnumerable<ClienteResponse>> BuscarPorNomeAsync(string nome);
}
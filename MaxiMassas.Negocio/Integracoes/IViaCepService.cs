namespace MaxiMassas.Negocio.Integracoes;

public interface IViaCepService
{
    Task<EnderecoDto?> BuscarEnderecoPorCepAsync(string cep);
}
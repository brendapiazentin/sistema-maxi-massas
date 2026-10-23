using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IClienteRepositorio : IRepositorio<Cliente>
{
    Task<IEnumerable<Cliente>> SearchByNomeAsync(string nome);
    Task<Cliente?> GetByEmailAsync(string email);
    /// <summary>Cliente com os endereços salvos, RASTREADO (para alterar e salvar).</summary>
    Task<Cliente?> GetComEnderecosAsync(int id);

    /// <summary>Cliente com conta no app (e-mail + senha), RASTREADO. Usado no "esqueci minha senha" do app.</summary>
    Task<Cliente?> GetContaAppPorEmailAsync(string emailMinusculo);
}
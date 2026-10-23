using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IProdutoRepositorio : IRepositorio<Produto>
{
    Task<Produto?> GetProdutoComDetalhesAsync(int id);
    Task<IEnumerable<Produto>> GetAllComEstoqueAsync();
}
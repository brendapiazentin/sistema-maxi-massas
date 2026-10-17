using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class ProdutoRepositorio : Repositorio<Produto>, IProdutoRepositorio
{
    public ProdutoRepositorio(AppDbContext context) : base(context) { }

    public async Task<Produto?> GetProdutoComDetalhesAsync(int id)
        => await _context.Produtos
            .Include(p => p.VariacoesProduto).Include(p => p.HistoricoPreco).Include(p => p.Estoque)
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Produto>> GetAllComEstoqueAsync()
        => await _context.Produtos.Include(p => p.Estoque).AsNoTracking().ToListAsync();
}
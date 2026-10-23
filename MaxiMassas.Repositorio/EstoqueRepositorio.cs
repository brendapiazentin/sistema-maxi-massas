using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class EstoqueRepositorio : Repositorio<Estoque>, IEstoqueRepositorio
{
    public EstoqueRepositorio(AppDbContext context) : base(context) { }

    public async Task<Estoque?> GetByProdutoIdAsync(int produtoId)
        => await _context.Estoques.Include(e => e.Produto).AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProdutoId == produtoId);

    public async Task<Estoque?> GetComMovimentacoesAsync(int estoqueId)
        => await _context.Estoques.Include(e => e.Produto).Include(e => e.Movimentacoes)
            .AsNoTracking().FirstOrDefaultAsync(e => e.Id == estoqueId);

    public Task<int> IncrementarAsync(int estoqueId, int quantidade)
        => _context.Estoques.Where(e => e.Id == estoqueId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantidade, e => e.Quantidade + quantidade));

    public Task<int> BaixarSeSuficienteAsync(int estoqueId, int quantidade)
        => _context.Estoques.Where(e => e.Id == estoqueId && e.Quantidade >= quantidade)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantidade, e => e.Quantidade - quantidade));

    public Task<int> IncrementarPorProdutoAsync(int produtoId, int quantidade)
        => _context.Estoques.Where(e => e.ProdutoId == produtoId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantidade, e => e.Quantidade + quantidade));

    public Task<int> BaixarPorProdutoSeSuficienteAsync(int produtoId, int quantidade)
        => _context.Estoques.Where(e => e.ProdutoId == produtoId && e.Quantidade >= quantidade)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantidade, e => e.Quantidade - quantidade));

    public Task DefinirQuantidadeAsync(int estoqueId, int novaQuantidade)
        => _context.Estoques.Where(e => e.Id == estoqueId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantidade, novaQuantidade));

    public Task<int> ObterQuantidadeAsync(int estoqueId)
        => _context.Estoques.Where(e => e.Id == estoqueId).Select(e => e.Quantidade).FirstAsync();

    public async Task<Estoque?> GetAtualizadoAsync(int estoqueId)
        => await _context.Estoques.AsNoTracking().FirstOrDefaultAsync(e => e.Id == estoqueId);

    public async Task<Estoque?> GetAtualizadoPorProdutoAsync(int produtoId)
        => await _context.Estoques.AsNoTracking().FirstOrDefaultAsync(e => e.ProdutoId == produtoId);

    public async Task<IEnumerable<Estoque>> GetEstoquesBaixosAsync()
        => await _context.Estoques.Include(e => e.Produto).AsNoTracking()
            .Where(e => e.Quantidade <= e.AlertaMinimo).ToListAsync();
}
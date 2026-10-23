using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IEstoqueRepositorio : IRepositorio<Estoque>
{
    Task<Estoque?> GetByProdutoIdAsync(int produtoId);
    Task<Estoque?> GetComMovimentacoesAsync(int estoqueId);
    Task<IEnumerable<Estoque>> GetEstoquesBaixosAsync();

    // ── Operações atômicas de quantidade (UPDATE direto no banco) ────────────
    // Quem garante que o estoque nunca fica negativo é o UPDATE condicional, não uma
    // conferência feita antes: o SQL Server trava a linha durante o UPDATE, então dois
    // pedidos simultâneos pelas últimas unidades são atendidos um de cada vez.

    /// <summary>Soma a quantidade ao saldo. Devolve as linhas alteradas (0 = estoque inexistente).</summary>
    Task<int> IncrementarAsync(int estoqueId, int quantidade);

    /// <summary>Subtrai a quantidade SE ainda houver saldo. Devolve as linhas alteradas (0 = saldo insuficiente).</summary>
    Task<int> BaixarSeSuficienteAsync(int estoqueId, int quantidade);

    /// <summary>Igual a <see cref="IncrementarAsync"/>, localizando o estoque pelo produto.</summary>
    Task<int> IncrementarPorProdutoAsync(int produtoId, int quantidade);

    /// <summary>Igual a <see cref="BaixarSeSuficienteAsync"/>, localizando o estoque pelo produto.</summary>
    Task<int> BaixarPorProdutoSeSuficienteAsync(int produtoId, int quantidade);

    /// <summary>Define uma quantidade exata (contagem de inventário).</summary>
    Task DefinirQuantidadeAsync(int estoqueId, int novaQuantidade);

    /// <summary>Quantidade atual, lida direto do banco (sem cache do contexto).</summary>
    Task<int> ObterQuantidadeAsync(int estoqueId);

    /// <summary>Estoque relido do banco (sem rastreamento): vê o resultado dos UPDATEs diretos acima.</summary>
    Task<Estoque?> GetAtualizadoAsync(int estoqueId);

    /// <summary>Igual a <see cref="GetAtualizadoAsync"/>, localizando pelo produto.</summary>
    Task<Estoque?> GetAtualizadoPorProdutoAsync(int produtoId);
}
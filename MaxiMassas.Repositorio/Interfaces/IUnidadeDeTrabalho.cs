namespace MaxiMassas.Repositorio.Interfaces;

/// <summary>
/// Transação do banco de dados. A camada de Negócio decide ONDE a transação começa e termina
/// (uma venda e a baixa do estoque são um bloco só), mas quem fala com o banco é o Repositório —
/// por isso a transação é entregue por esta interface, e a Negócio nunca vê o DbContext.
/// </summary>
public interface ITransacao : IAsyncDisposable
{
    Task CommitAsync();
}

public interface IUnidadeDeTrabalho
{
    /// <summary>Abre uma transação. Descartar sem chamar CommitAsync desfaz tudo (rollback).</summary>
    Task<ITransacao> IniciarTransacaoAsync();
}

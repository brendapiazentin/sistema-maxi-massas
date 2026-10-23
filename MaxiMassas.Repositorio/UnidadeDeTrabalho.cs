using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace MaxiMassas.Repositorio;

public class UnidadeDeTrabalho : IUnidadeDeTrabalho
{
    private readonly AppDbContext _context;

    public UnidadeDeTrabalho(AppDbContext context) => _context = context;

    public async Task<ITransacao> IniciarTransacaoAsync()
        => new Transacao(await _context.Database.BeginTransactionAsync());

    private sealed class Transacao : ITransacao
    {
        private readonly IDbContextTransaction _transacao;

        public Transacao(IDbContextTransaction transacao) => _transacao = transacao;

        public Task CommitAsync() => _transacao.CommitAsync();

        public ValueTask DisposeAsync() => _transacao.DisposeAsync();
    }
}

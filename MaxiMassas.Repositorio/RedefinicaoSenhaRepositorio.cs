using MaxiMassas.Modelos;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class RedefinicaoSenhaRepositorio : Repositorio<PasswordResetToken>, IRedefinicaoSenhaRepositorio
{
    public RedefinicaoSenhaRepositorio(AppDbContext context) : base(context) { }

    public async Task<PasswordResetToken?> GetTokenAbertoUsuarioAsync(string hash)
        => await _context.PasswordResetTokens.FirstOrDefaultAsync(t =>
            t.Destino == DestinoRedefinicao.Usuario && t.Token == hash && !t.Usado && t.ExpiresAt > DateTime.Now);

    public async Task<PasswordResetToken?> GetCodigoAbertoClienteAsync(int clienteId)
        => await _context.PasswordResetTokens
            .Where(t => t.Destino == DestinoRedefinicao.Cliente && t.ReferenciaId == clienteId && !t.Usado && t.ExpiresAt > DateTime.Now)
            .OrderByDescending(t => t.CriadoEm)
            .FirstOrDefaultAsync();

    public Task<bool> HouvePedidoDepoisDeAsync(DestinoRedefinicao destino, int referenciaId, DateTime limite)
        => _context.PasswordResetTokens.AnyAsync(t =>
            t.Destino == destino && t.ReferenciaId == referenciaId && t.CriadoEm > limite);

    public async Task InvalidarAbertosAsync(DestinoRedefinicao destino, int referenciaId)
    {
        var abertos = await _context.PasswordResetTokens
            .Where(t => t.Destino == destino && t.ReferenciaId == referenciaId && !t.Usado)
            .ToListAsync();
        abertos.ForEach(t => t.Usado = true);
    }
}

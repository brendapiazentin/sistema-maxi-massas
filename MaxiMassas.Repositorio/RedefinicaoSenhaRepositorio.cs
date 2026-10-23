using MaxiMassas.Modelos;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class RedefinicaoSenhaRepositorio : Repositorio<PasswordResetToken>, IRedefinicaoSenhaRepositorio
{
    public RedefinicaoSenhaRepositorio(AppDbContext context) : base(context) { }
    private static DateTime Agora => DateTime.Now; // hora local do sistema, enviada como parametro (DateTime.Now direto na consulta viraria GETDATE() do banco, que esta em UTC)

    public async Task<PasswordResetToken?> GetTokenAbertoUsuarioAsync(string hash)
        => await _context.PasswordResetTokens.FirstOrDefaultAsync(t =>
            t.Destino == DestinoRedefinicao.Usuario && t.Token == hash && !t.Usado && t.ExpiresAt > Agora);

    public async Task<PasswordResetToken?> GetCodigoAbertoClienteAsync(int clienteId)
        => await _context.PasswordResetTokens
            .Where(t => t.Destino == DestinoRedefinicao.Cliente && t.ReferenciaId == clienteId && !t.Usado && t.ExpiresAt > Agora)
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

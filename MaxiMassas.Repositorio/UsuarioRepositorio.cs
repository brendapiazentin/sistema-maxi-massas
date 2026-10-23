using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class UsuarioRepositorio : Repositorio<Usuario>, IUsuarioRepositorio
{
    public UsuarioRepositorio(AppDbContext context) : base(context) { }

    public async Task<Usuario?> GetByEmailOuLoginAsync(string textoMinusculo)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == textoMinusculo || u.Login.ToLower() == textoMinusculo);

    public async Task<IEnumerable<Usuario>> ListarPorNomeAsync()
        => await _context.Usuarios.AsNoTracking().OrderBy(u => u.Nome).ToListAsync();

    public async Task<bool> PossuiHistoricoAsync(int usuarioId)
        => await _context.Vendas.AnyAsync(v => v.UsuarioId == usuarioId)
           || await _context.ConsumosProprios.AnyAsync(c => c.UsuarioId == usuarioId);

    public Task<bool> ExisteOutroAdminAtivoAsync(int idExcluido)
        => _context.Usuarios.AnyAsync(u => u.Id != idExcluido && u.Ativo && u.Tipo == TipoUsuario.Administrador);

    public Task<bool> EmailEmUsoAsync(string email, int? idAtual)
        => _context.Usuarios.AnyAsync(u => u.Email == email && u.Id != idAtual);

    public async Task<(byte[] Foto, DateTime AtualizadaEm)?> ObterFotoAsync(int usuarioId)
    {
        var dados = await _context.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Foto != null)
            .Select(u => new { u.Foto, u.FotoAtualizadaEm })
            .FirstOrDefaultAsync();
        return dados is null ? null : (dados.Foto!, dados.FotoAtualizadaEm ?? DateTime.MinValue);
    }

    public async Task<Usuario?> ObterResumoSessaoAsync(int usuarioId)
        => await _context.Usuarios.AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new Usuario { Id = u.Id, Login = u.Login, Nome = u.Nome, Tipo = u.Tipo, Ativo = u.Ativo, FotoAtualizadaEm = u.FotoAtualizadaEm })
            .FirstOrDefaultAsync();

    public async Task<Usuario?> GetByLoginAsync(string login)
        => await _context.Usuarios.FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower());
}
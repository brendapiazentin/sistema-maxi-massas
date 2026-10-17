using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class BackupRepositorio : IBackupRepositorio
{
    private readonly AppDbContext _context;

    public BackupRepositorio(AppDbContext context) => _context = context;

    public async Task ExecutarBackupAsync(string caminhoCompleto, string nomeDoBackup, CancellationToken ct = default)
    {
        // O nome do banco vem da connection string; só os colchetes precisam de escape.
        var banco = _context.Database.GetDbConnection().Database.Replace("]", "]]");

        // Caminho e nome vão como parâmetros (nunca concatenados no SQL).
        var sql = $"BACKUP DATABASE [{banco}] TO DISK = @Caminho WITH COMPRESSION, STATS = 10, NAME = @Nome";

        // Timeout generoso para bancos maiores (5 minutos).
        _context.Database.SetCommandTimeout(300);
        await _context.Database.ExecuteSqlRawAsync(
            sql,
            new object[] { new SqlParameter("@Caminho", caminhoCompleto), new SqlParameter("@Nome", nomeDoBackup) },
            ct);
    }
}

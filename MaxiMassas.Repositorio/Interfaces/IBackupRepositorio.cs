namespace MaxiMassas.Repositorio.Interfaces;

public interface IBackupRepositorio
{
    /// <summary>
    /// Executa BACKUP DATABASE (completo, comprimido) do banco da connection string, gravando em
    /// <paramref name="caminhoCompleto"/>. O caminho vale no computador do SQL Server.
    /// </summary>
    Task ExecutarBackupAsync(string caminhoCompleto, string nomeDoBackup, CancellationToken ct = default);
}

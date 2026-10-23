using System.Data;
using Dapper;
using MaxiMassas.Repositorio.Contexto;
using MaxiMassas.Repositorio.Interfaces;
using MaxiMassas.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MaxiMassas.Repositorio;

public class ClienteRepositorio : Repositorio<Cliente>, IClienteRepositorio
{
    public ClienteRepositorio(AppDbContext context) : base(context) { }

    // Dapper + stored procedure (exigência do manual do PIM IV: a procedure é executada pelo sistema).
    // O parâmetro vai separado do comando: isso é o que impede SQL Injection.
    public async Task<IEnumerable<Cliente>> SearchByNomeAsync(string nome)
    {
        // Reaproveita a mesma conexão (e a mesma connection string) do EF Core.
        var conexao = _context.Database.GetDbConnection();
        return await conexao.QueryAsync<Cliente>(
            "sp_Clientes_BuscarPorNome",
            new { Trecho = nome },
            commandType: CommandType.StoredProcedure);
    }

    public async Task<Cliente?> GetByEmailAsync(string email)
        => await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Email == email.Trim().ToLower());

    public async Task<Cliente?> GetContaAppPorEmailAsync(string emailMinusculo)
        => await _context.Clientes.FirstOrDefaultAsync(c => c.Email == emailMinusculo && c.SenhaHash != null);

    public async Task<Cliente?> GetComEnderecosAsync(int id)
        => await _context.Clientes.Include(c => c.Enderecos).FirstOrDefaultAsync(c => c.Id == id);
}
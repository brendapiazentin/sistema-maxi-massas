using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

public interface IUsuarioRepositorio : IRepositorio<Usuario>
{
    Task<Usuario?> GetByLoginAsync(string login);

    /// <summary>Usuário pelo e-mail OU pelo login (já em minúsculas), rastreado. Usado no "esqueci minha senha".</summary>
    Task<Usuario?> GetByEmailOuLoginAsync(string textoMinusculo);

    /// <summary>Todos os usuários, por nome, sem rastreamento.</summary>
    Task<IEnumerable<Usuario>> ListarPorNomeAsync();

    /// <summary>O usuário tem vendas ou consumos próprios registrados? (então só pode ser desativado, não excluído)</summary>
    Task<bool> PossuiHistoricoAsync(int usuarioId);

    /// <summary>Existe algum administrador ativo além deste?</summary>
    Task<bool> ExisteOutroAdminAtivoAsync(int idExcluido);

    /// <summary>O e-mail já pertence a outro usuário?</summary>
    Task<bool> EmailEmUsoAsync(string email, int? idAtual);

    /// <summary>Só a foto e a data dela (a foto não vem junto com o usuário nas outras consultas).</summary>
    Task<(byte[] Foto, DateTime AtualizadaEm)?> ObterFotoAsync(int usuarioId);

    /// <summary>Dados leves para validar a sessão do painel a cada requisição (sem a foto).</summary>
    Task<Usuario?> ObterResumoSessaoAsync(int usuarioId);
}
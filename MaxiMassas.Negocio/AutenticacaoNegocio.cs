using System.Security.Claims;
using MaxiMassas.Modelos;
using MaxiMassas.Repositorio.Interfaces;

namespace MaxiMassas.Negocio;

/// <summary>Conferência de credenciais de usuários do painel e de clientes do app.</summary>
public interface IAutenticacaoNegocio
{
    /// <summary>Usuário ativo cujo login e senha conferem; null caso contrário.</summary>
    Task<Usuario?> AutenticarUsuarioAsync(string login, string senha);

    /// <summary>Cliente com conta no app cujo e-mail e senha conferem; null caso contrário.</summary>
    Task<Cliente?> AutenticarClienteAsync(string email, string senha);

    /// <summary>Dados leves do usuário (sem foto) para revalidar a sessão do painel a cada requisição.</summary>
    Task<Usuario?> ObterResumoSessaoAsync(int usuarioId);
}

public class AutenticacaoNegocio : IAutenticacaoNegocio
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IClienteRepositorio _clientes;

    public AutenticacaoNegocio(IUsuarioRepositorio usuarios, IClienteRepositorio clientes)
    { _usuarios = usuarios; _clientes = clientes; }

    public async Task<Usuario?> AutenticarUsuarioAsync(string login, string senha)
    {
        var usuario = await _usuarios.GetByLoginAsync(login);
        // Desativado não entra (continua no banco só por causa do histórico).
        if (usuario is null || !usuario.Ativo || !BCrypt.Net.BCrypt.Verify(senha, usuario.Senha)) return null;
        return usuario;
    }

    public async Task<Cliente?> AutenticarClienteAsync(string email, string senha)
    {
        var cliente = await _clientes.GetByEmailAsync(email);
        if (cliente?.SenhaHash is null || !BCrypt.Net.BCrypt.Verify(senha, cliente.SenhaHash)) return null;
        return cliente;
    }

    public Task<Usuario?> ObterResumoSessaoAsync(int usuarioId) => _usuarios.ObterResumoSessaoAsync(usuarioId);
}

/// <summary>Claims do usuário do painel; iguais no Basic Auth da Api e no cookie da Web.</summary>
public static class PrincipalUsuario
{
    /// <summary>Claim com a versão da foto (ticks); vazia = sem foto. O menu monta a URL da imagem com ela.</summary>
    public const string ClaimFoto = "maximassas:foto";

    public static ClaimsPrincipal Criar(Usuario usuario, string esquema)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Login),
            new Claim(ClaimTypes.GivenName, usuario.Nome),
            new Claim(ClaimTypes.Role, usuario.Tipo.ToString()),
            new Claim(ClaimFoto, VersaoFoto(usuario.FotoAtualizadaEm))
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, esquema));
    }

    public static string VersaoFoto(DateTime? atualizadaEm) => atualizadaEm?.Ticks.ToString() ?? string.Empty;
}

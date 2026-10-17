using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

/// <summary>Tokens e códigos do "esqueci minha senha" (painel e app).</summary>
public interface IRedefinicaoSenhaRepositorio : IRepositorio<PasswordResetToken>
{
    /// <summary>Token do link do painel que ainda vale (não usado e não expirado). Rastreado.</summary>
    Task<PasswordResetToken?> GetTokenAbertoUsuarioAsync(string hash);

    /// <summary>Código mais recente, ainda aberto, de um cliente do app. Rastreado.</summary>
    Task<PasswordResetToken?> GetCodigoAbertoClienteAsync(int clienteId);

    /// <summary>Já houve pedido para esta pessoa depois do instante informado?</summary>
    Task<bool> HouvePedidoDepoisDeAsync(DestinoRedefinicao destino, int referenciaId, DateTime limite);

    /// <summary>Marca como usados todos os pedidos ainda abertos da pessoa (não salva).</summary>
    Task InvalidarAbertosAsync(DestinoRedefinicao destino, int referenciaId);
}

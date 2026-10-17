// RF04 — Contrato do repositório de Consumo Próprio
using MaxiMassas.Modelos;

namespace MaxiMassas.Repositorio.Interfaces;

/// <summary>
/// Abstração para acesso a dados de ConsumoProprio.
/// Herda as operações genéricas de IRepositorio e adiciona consultas específicas.
/// </summary>
public interface IConsumoProprioRepositorio : IRepositorio<ConsumoProprio>
{
    /// <summary>Retorna todos os consumos com dados do produto e usuário carregados.</summary>
    Task<IEnumerable<ConsumoProprio>> GetAllComDetalhesAsync();

    /// <summary>Retorna os consumos filtrados por período.</summary>
    Task<IEnumerable<ConsumoProprio>> GetByPeriodoAsync(DateTime inicio, DateTime fim);

    /// <summary>Retorna os consumos de um produto específico.</summary>
    Task<IEnumerable<ConsumoProprio>> GetByProdutoIdAsync(int produtoId);
}

// ViewModel da tela "Estoque": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

public class ItemReposicaoForm
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public class EstoqueViewModel
{
    [BindNever] public IReadOnlyList<ProdutoResponse> Produtos { get; set; } = Array.Empty<ProdutoResponse>();

    /// <summary>Produtos agrupados por nome, como nos cartões do modal de Reposição.</summary>
    public IEnumerable<IGrouping<string, ProdutoResponse>> GruposReposicao => Produtos.GroupBy(p => p.Nome);

    /// <summary>Mensagem de erro de uma ação recusada (mostrada no topo da página).</summary>
    [BindNever] public string? Erro { get; set; }

    /// <summary>Ajustar para uma quantidade exata é só para o gestor (Administrador); a reposição é para todos.</summary>
    [BindNever] public bool PodeAjustar { get; set; }

    public static string NomeCompleto(ProdutoResponse p) => string.IsNullOrEmpty(p.Sabor) ? p.Nome : $"{p.Nome} {p.Sabor}";
}

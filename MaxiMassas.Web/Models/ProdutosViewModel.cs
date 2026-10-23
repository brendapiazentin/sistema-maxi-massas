// ViewModel da tela "Produtos": o que a view mostra e o que o formulario envia.
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MaxiMassas.Web.Models;

public class ProdutoForm
{
    /// <summary>0 = produto novo.</summary>
    public int Id { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Nome { get; set; } = string.Empty;
    public string? Sabor { get; set; }
    public string? Peso { get; set; }
    public string? Custo { get; set; }
    public string? Preco { get; set; }
    /// <summary>Só no cadastro: estoque inicial.</summary>
    public int? Estoque { get; set; }
    /// <summary>Nome do enum TipoProduto (ex.: "Massa").</summary>
    public string? Tipo { get; set; }
    public string? Observacao { get; set; }
    /// <summary>Inativo = produto retirado do cardapio do app.</summary>
    public bool Ativo { get; set; } = true;
}

public class ProdutosViewModel
{
    [BindNever] public IReadOnlyList<ProdutoResponse> Produtos { get; set; } = Array.Empty<ProdutoResponse>();

    public ProdutoForm Produto { get; set; } = new();

    [BindNever] public IReadOnlyList<string> ErrosFormulario { get; set; } = Array.Empty<string>();

    /// <summary>Cadastrar e alterar produto é só para Administrador (mesma regra do POST/PUT /api/Produto).</summary>
    [BindNever] public bool PodeEditar { get; set; }
}

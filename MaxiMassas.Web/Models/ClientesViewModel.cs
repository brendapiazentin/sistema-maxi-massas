// ViewModel da tela "Clientes": o que a view mostra e o que o formulario envia.
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MaxiMassas.Web.Models;

public class ClienteForm
{
    /// <summary>0 = cliente novo.</summary>
    public int Id { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Nome { get; set; } = string.Empty;
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Telefone { get; set; } = string.Empty;
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Cep { get; set; } = string.Empty;
    public string? Rua { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Numero { get; set; } = string.Empty;
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Complemento { get; set; }
    public string? Observacao { get; set; }
}

public record ClienteLinha(ClienteResponse Cliente, int Compras, decimal TotalGasto);

public class ClientesViewModel
{
    [BindNever] public IReadOnlyList<ClienteLinha> Linhas { get; set; } = Array.Empty<ClienteLinha>();

    public ClienteForm Cliente { get; set; } = new();

    /// <summary>Erros do último envio — a página reabre o modal com o que foi digitado.</summary>
    [BindNever] public IReadOnlyList<string> ErrosFormulario { get; set; } = Array.Empty<string>();
}

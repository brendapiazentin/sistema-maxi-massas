// ViewModel da tela "Pdv": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace MaxiMassas.Web.Models;

/// <summary>Campos ocultos do formulário do PDV, preenchidos pelo pdv.js antes de enviar.</summary>
public class VendaForm
{
    public int ClienteId { get; set; }
    /// <summary>[{"produtoId":1,"quantidade":2}]</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string ItensJson { get; set; } = "[]";
    /// <summary>"Retirada" ou "Delivery" (rótulos dos botões da tela).</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string TipoEntrega { get; set; } = string.Empty;
    public string? DataAgendada { get; set; }
    public string? HoraAgendada { get; set; }
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Complemento { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Plataforma { get; set; } = string.Empty;
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Pagamento { get; set; } = string.Empty;
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Status { get; set; } = string.Empty;
    /// <summary>"RS" ou "PCT".</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string TipoDesconto { get; set; } = "RS";
    /// <summary>Texto com ponto decimal (vem do JavaScript), lido com cultura invariante.</summary>
    public string? Desconto { get; set; }
    public string? Observacao { get; set; }
}

public record ItemForm(int ProdutoId, int Quantidade);

public class PdvViewModel
{
    /// <summary>Id da venda editada (rota /AlterarVenda/{id}); 0 na nova venda.</summary>
    public int Id { get; set; }

    public VendaForm Venda { get; set; } = new();

    /// <summary>Título da tela ("Nova Venda" / "Editar Venda").</summary>
    [BindNever] public string Titulo { get; set; } = string.Empty;

    /// <summary>Mensagem de erro do último envio (estoque insuficiente, campo faltando...).</summary>
    [BindNever] public string? Erro { get; set; }

    /// <summary>Dados para o pdv.js: catálogo, regra da taxa e o estado da comanda a restaurar.</summary>
    [BindNever] public string DadosJson { get; set; } = "{}";
}

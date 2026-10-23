// ViewModel da tela "Entregas": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

/// <summary>Endereço exibido de uma entrega: o do pedido (app/PDV) ou, sem ele, o do cadastro do cliente.</summary>
public record EnderecoEntrega(string Rua, string Numero, string Bairro, string Complemento, string Cidade);

public record LinhaEntrega(VendaResponse Venda, EnderecoEntrega Endereco);

public class EntregasViewModel
{
    [BindNever] public IReadOnlyList<LinhaEntrega> Linhas { get; set; } = Array.Empty<LinhaEntrega>();

    [BindNever] public IReadOnlyList<int> Anos { get; set; } = Array.Empty<int>();

    [BindNever] public ConfiguracaoEntregaResponse Taxa { get; set; } = new();

    [BindNever] public bool PodeAlterarTaxa { get; set; }

    /// <summary>Valores digitados no modal da taxa e o erro, quando o envio é recusado.</summary>
    [BindNever] public string? TaxaDigitada { get; set; }

    [BindNever] public string? MinimoDigitado { get; set; }

    [BindNever] public string? ErroTaxa { get; set; }

    /// <summary>Mesmos rótulos de antes: Concluído, Cancelado e o resto "Pendente".</summary>
    public static string RotuloStatus(StatusVenda s) => s switch
    {
        StatusVenda.Concluida => "Concluído",
        StatusVenda.Cancelada => "Cancelado",
        _ => "Pendente"
    };

    public static string Ou(string? texto) => string.IsNullOrWhiteSpace(texto) ? "—" : texto;
}

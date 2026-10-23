// ViewModel da tela "Vendas": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace MaxiMassas.Web.Models;

public class VendasViewModel
{
    [BindNever] public IReadOnlyList<VendaResponse> Vendas { get; set; } = Array.Empty<VendaResponse>();

    [BindNever] public IReadOnlyDictionary<int, string> TelefonePorCliente { get; set; } = new Dictionary<int, string>();

    /// <summary>Opções dos filtros, tiradas dos dados (antes eram fixas no HTML).</summary>
    [BindNever] public IReadOnlyList<int> Anos { get; set; } = Array.Empty<int>();

    [BindNever] public IReadOnlyList<string> NomesProdutos { get; set; } = Array.Empty<string>();

    /// <summary>Soma das vendas que valeram (canceladas não entram).</summary>
    [BindNever] public decimal Total { get; set; }

    /// <summary>Produtos para a busca do modal de Consumo próprio (lidos pelo main.js).</summary>
    [BindNever] public string ProdutosConsumoJson { get; set; } = "[]";

    [BindNever] public string? ErroConsumo { get; set; }

    /// <summary>Consumo próprio é só para Administrador (mesma regra da API /api/ConsumoProprio).</summary>
    [BindNever] public bool PodeRegistrarConsumo { get; set; }

    // ── Textos das colunas (mesmos rótulos que o vendas.js usava) ────────────

    public static string RotuloStatus(StatusVenda status) => status switch
    {
        StatusVenda.Paga => "Pago",
        StatusVenda.EmEntrega => "Agendado",
        StatusVenda.Concluida => "Concluído",
        StatusVenda.Cancelada => "Cancelado",
        _ => "Pendente"
    };

    public static string RotuloPagamento(FormaPagamento? forma) => forma switch
    {
        FormaPagamento.Pix => "PIX",
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.CartaoDebito => "Cartão Débito",
        FormaPagamento.CartaoCredito => "Cartão Crédito",
        FormaPagamento.Transferencia => "Transferência",
        FormaPagamento.Outro => "Outro",
        _ => "—"
    };

    public static string TextoDesconto(VendaResponse v) => v.Desconto <= 0
        ? "R$ —"
        : v.TipoDesconto == TipoDesconto.Percentual ? $"{v.Desconto:0.##}%" : PainelFormatos.Moeda(v.Desconto);

    public static string TextoEntrega(VendaResponse v) => v.Entrega?.TipoEntrega == TipoEntrega.Entrega ? "Entrega" : "Retirada";
}

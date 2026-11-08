// US06 — Taxas descontadas do faturamento do proprietário, conforme a forma de pagamento e a plataforma.
//
//   Cartão de débito .................. 1,37%
//   Cartão de crédito ................. 3,15%
//   UAI Rango pago pela plataforma .... 13,5%  (forma de pagamento "Pago pela plataforma")
//   UAI Rango pago na entrega/retirada  10%    (qualquer outra forma de pagamento) + a taxa do cartão, se for cartão
//   Pix, dinheiro e demais ............ sem taxa
//
// O percentual incide sobre o valor total da venda (Venda.ValorTotal, já com desconto e frete).
// A taxa é calculada na hora do relatório, a partir da forma de pagamento e da plataforma atuais da venda:
// assim, trocar a forma de pagamento ao marcar um pedido como pago reflete no lucro.
using MaxiMassas.Modelos;

namespace MaxiMassas.Negocio;

public static class TaxasPagamento
{
    public const decimal CartaoDebito = 1.37m;
    public const decimal CartaoCredito = 3.15m;
    public const decimal UaiRangoPagoPelaPlataforma = 13.5m;
    public const decimal UaiRangoPagoNaEntrega = 10m;

    /// <summary>A plataforma de origem é o UAI Rango? (valor gravado pelo PDV: "UAI Rango").</summary>
    public static bool EhUaiRango(string? origem)
        => string.Equals(origem?.Trim(), "UAI Rango", StringComparison.OrdinalIgnoreCase);

    /// <summary>Percentual (ex.: 3.15 = 3,15%) descontado do faturamento da venda.</summary>
    public static decimal Percentual(string? origem, FormaPagamento? forma)
    {
        if (forma == FormaPagamento.PlataformaUaiRango) return UaiRangoPagoPelaPlataforma;

        var cartao = forma switch
        {
            FormaPagamento.CartaoDebito => CartaoDebito,
            FormaPagamento.CartaoCredito => CartaoCredito,
            _ => 0m
        };
        return EhUaiRango(origem) ? UaiRangoPagoNaEntrega + cartao : cartao;
    }

    /// <summary>Valor em reais da taxa da venda (zero se a venda ainda não tem pagamento).</summary>
    public static decimal Valor(Venda venda)
        => Valor(venda.ValorTotal, venda.Origem, venda.Pagamento?.FormaPagamento);

    /// <summary>Mesma taxa, a partir dos dados da venda (a lista de Vendas trabalha com o DTO).</summary>
    public static decimal Valor(decimal valorTotal, string? origem, FormaPagamento? forma)
        => Math.Round(valorTotal * Percentual(origem, forma) / 100m, 2, MidpointRounding.AwayFromZero);
}

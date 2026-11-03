using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;

namespace MaxiMassas.Modelos.Validadores;

public class ConfiguracaoEntregaRequestValidator : AbstractValidator<ConfiguracaoEntregaRequest>
{
    public ConfiguracaoEntregaRequestValidator()
    {
        RuleFor(c => c.TaxaEntrega).GreaterThanOrEqualTo(0).WithMessage("A taxa de entrega nao pode ser negativa.")
            .LessThan(10000);
        RuleFor(c => c.PedidoMinimoEntregaGratis).GreaterThanOrEqualTo(0)
            .WithMessage("O pedido minimo para entrega gratis nao pode ser negativo.").LessThan(100000);
        RuleFor(c => c.TelegramEntregador1).Matches(@"^-?\d{5,20}$").When(c => !string.IsNullOrWhiteSpace(c.TelegramEntregador1))
            .WithMessage("O identificador do Telegram do entregador 1 deve conter apenas numeros.");
        RuleFor(c => c.TelegramEntregador2).Matches(@"^-?\d{5,20}$").When(c => !string.IsNullOrWhiteSpace(c.TelegramEntregador2))
            .WithMessage("O identificador do Telegram do entregador 2 deve conter apenas numeros.");
    }
}

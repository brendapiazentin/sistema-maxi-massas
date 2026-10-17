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
    }
}

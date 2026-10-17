// RF04 — Validação do ConsumoProprioRequest com FluentValidation
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;

namespace MaxiMassas.Modelos.Validadores;

/// <summary>
/// Regras de validação para o request de Consumo Próprio.
/// FluentValidation executa antes do controller ser ativado (via AutoValidation).
/// </summary>
public class ConsumoProprioValidator : AbstractValidator<ConsumoProprioRequest>
{
    public ConsumoProprioValidator()
    {
        RuleFor(x => x.ProdutoId)
            .GreaterThan(0).WithMessage("ProdutoId deve ser um ID válido (> 0).");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.")
            .LessThanOrEqualTo(10000).WithMessage("A quantidade máxima por consumo é 10.000 unidades.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("A observação deve ter no máximo 500 caracteres.")
            .When(x => x.Observacao != null);
    }
}

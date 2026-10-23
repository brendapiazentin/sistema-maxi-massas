// ViewModel da tela "RedefinirSenha": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

public class RedefinirSenhaViewModel
{
    public string Token { get; set; } = string.Empty;

    public string NovaSenha { get; set; } = string.Empty;

    public string ConfirmarSenha { get; set; } = string.Empty;

    [BindNever] public bool LinkValido { get; set; }

    [BindNever] public string? Erro { get; set; }
}

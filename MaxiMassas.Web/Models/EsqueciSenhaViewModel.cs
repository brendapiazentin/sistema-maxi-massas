// ViewModel da tela "EsqueciSenha": o que a view mostra e o que o formulario envia.
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

public class EsqueciSenhaViewModel
{
    public string? EmailOuLogin { get; set; }

    [BindNever] public bool Enviado { get; set; }

    [BindNever] public string? Erro { get; set; }
}

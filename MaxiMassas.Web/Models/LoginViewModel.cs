// ViewModel da tela "Login": o que a view mostra e o que o formulario envia.
using MaxiMassas.Web.Seguranca;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Models;

public class LoginViewModel
{
    public string Usuario { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;

        /// <summary>Página que pediu o login (ex.: /Vendas); volta para ela depois de entrar.</summary>
    public string? ReturnUrl { get; set; }

    [BindNever] public string? Erro { get; set; }
}

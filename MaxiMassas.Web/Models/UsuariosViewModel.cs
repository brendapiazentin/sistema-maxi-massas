// ViewModel da tela "Usuarios": o que a view mostra e o que o formulario envia.
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MaxiMassas.Web.Models;

public class UsuarioForm
{
    /// <summary>0 = usuário novo.</summary>
    public int Id { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Nome { get; set; } = string.Empty;
    /// <summary>Só no cadastro (depois não muda).</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Login { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public TipoUsuario Tipo { get; set; } = TipoUsuario.Operador;
    /// <summary>Obrigatória no cadastro; na edição, preenchida = redefinir.</summary>
    public string? Senha { get; set; }
}

public class UsuariosViewModel
{
    [BindNever] public IReadOnlyList<UsuarioResponse> Usuarios { get; set; } = Array.Empty<UsuarioResponse>();

    public UsuarioForm Usuario { get; set; } = new();

    [BindNever] public IReadOnlyList<string> ErrosFormulario { get; set; } = Array.Empty<string>();

    [BindNever] public string? Erro { get; set; }

    [BindNever] public int MeuId { get; set; }
}

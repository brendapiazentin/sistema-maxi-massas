// ViewModel da tela "Perfil": o que a view mostra e o que o formulario envia.
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MaxiMassas.Web.Models;

// Foto de até 2 MB (+ folga para o resto do formulário).

public class PerfilViewModel
{
    [BindNever] public UsuarioResponse Usuario { get; set; } = new();

    public string Nome { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Telefone { get; set; }

    /// <summary>Erro de cada bloco da tela (aparece dentro do próprio bloco).</summary>
    [BindNever] public string? ErroDados { get; set; }

    [BindNever] public string? ErroFoto { get; set; }

    [BindNever] public string? ErroSenha { get; set; }
}

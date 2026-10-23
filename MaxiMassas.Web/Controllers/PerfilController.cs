// "Meu perfil": cada usuário do painel muda os próprios dados, a foto e a senha.
// (Perfil de acesso e login só o administrador muda, na tela de Usuários.)
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MaxiMassas.Web.Controllers;

[RequestSizeLimit(3 * 1024 * 1024)]
[RequestFormLimits(MultipartBodyLengthLimit = 3 * 1024 * 1024)]
[Route("Perfil")]
public class PerfilController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public PerfilViewModel Vm { get; set; } = new();

    private readonly IUsuarioNegocio _usuarios;

    private readonly IFotoPerfilService _fotos;

    private readonly IValidator<PerfilUsuarioRequest> _validador;

    public PerfilController(IUsuarioNegocio usuarios, IFotoPerfilService fotos, IValidator<PerfilUsuarioRequest> validador)
    {
        _usuarios = usuarios;
        _fotos = fotos;
        _validador = validador;
    }

    private int MeuId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<IActionResult> ComErroAsync(Action<string> definir, string mensagem, bool manterDigitado = false)
    {
        definir(mensagem);
        await CarregarAsync();
        // Erro no bloco de dados: volta o que foi digitado. Nos outros blocos (foto, senha), os dados não
        // vieram no formulário — mostra os gravados.
        if (!manterDigitado)
        {
            Vm.Nome = Vm.Usuario.Nome;
            Vm.Email = Vm.Usuario.Email;
            Vm.Telefone = Vm.Usuario.Telefone;
        }
        return View("Index", Vm);
    }

    private async Task<bool> CarregarAsync()
    {
        var usuario = await _usuarios.GetByIdAsync(MeuId);
        if (usuario is null) return false;
        Vm.Usuario = usuario;
        return true;
    }

    private static string Traduzir(string mensagem) => mensagem
        .Replace("e obrigatorio", "é obrigatório").Replace("invalido", "inválido");

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!await CarregarAsync()) return RedirectToAction("Index", "Logout");
        Vm.Nome = Vm.Usuario.Nome;
        Vm.Email = Vm.Usuario.Email;
        Vm.Telefone = Vm.Usuario.Telefone;
        return View("Index", Vm);
    }

    [HttpPost("Dados")]
    public async Task<IActionResult> Dados()
    {
        var request = new PerfilUsuarioRequest { Nome = Vm.Nome.Trim(), Email = Vm.Email, Telefone = Vm.Telefone };
        var validacao = await _validador.ValidateAsync(request);
        if (!validacao.IsValid) return await ComErroAsync(e => Vm.ErroDados = e, Traduzir(validacao.Errors[0].ErrorMessage), manterDigitado: true);

        try
        {
            await _usuarios.AtualizarPerfilAsync(MeuId, request);
        }
        catch (InvalidOperationException ex)
        {
            return await ComErroAsync(e => Vm.ErroDados = e, ex.Message, manterDigitado: true);
        }
        TempData["Mensagem"] = "Seus dados foram atualizados.";
        return RedirectToAction("Index");
    }

    [HttpPost("Foto")]
    public async Task<IActionResult> Foto(IFormFile? foto)
    {
        if (foto is null || foto.Length == 0) return await ComErroAsync(e => Vm.ErroFoto = e, "Escolha uma imagem.");
        try
        {
            await using var arquivo = foto.OpenReadStream();
            var preparada = await _fotos.PrepararAsync(arquivo, foto.Length);
            await _usuarios.DefinirFotoAsync(MeuId, preparada);
        }
        catch (InvalidOperationException ex)
        {
            return await ComErroAsync(e => Vm.ErroFoto = e, ex.Message);
        }
        TempData["Mensagem"] = "Foto atualizada.";
        return RedirectToAction("Index");
    }

    [HttpPost("RemoverFoto")]
    public async Task<IActionResult> RemoverFoto()
    {
        await _usuarios.DefinirFotoAsync(MeuId, null);
        TempData["Mensagem"] = "Foto removida.";
        return RedirectToAction("Index");
    }

    [HttpPost("Senha")]
    public async Task<IActionResult> Senha(string? senhaAtual, string? novaSenha, string? confirmacao)
    {
        if (novaSenha != confirmacao) return await ComErroAsync(e => Vm.ErroSenha = e, "A confirmação não confere com a nova senha.");
        try
        {
            await _usuarios.TrocarSenhaAsync(MeuId, senhaAtual ?? string.Empty, novaSenha ?? string.Empty);
        }
        catch (InvalidOperationException ex)
        {
            return await ComErroAsync(e => Vm.ErroSenha = e, ex.Message);
        }
        TempData["Mensagem"] = "Senha alterada. Use a nova senha no próximo acesso.";
        return RedirectToAction("Index");
    }
}

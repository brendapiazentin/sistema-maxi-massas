// Usuários do painel — só para Administrador: cadastrar, editar (nome, contato, perfil),
// redefinir a senha de alguém e ativar/desativar. As regras (login único, senha forte, nunca
// ficar sem administrador ativo) estão no UsuarioNegocio, o mesmo da API /api/Usuario.
using FluentValidation;
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MaxiMassas.Web.Controllers;

[Route("Usuarios")]
public class UsuariosController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public UsuariosViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.MeuId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Operador que abrir o endereço volta ao Dashboard com o aviso.
        if (!User.IsInRole(nameof(TipoUsuario.Administrador)))
        {
            TempData["Mensagem"] = "A tela de Usuários é só para administradores.";
            context.Result = RedirectToAction("Index", "Dashboard");
            return;
        }
        base.OnActionExecuting(context);
    }

    private readonly IUsuarioNegocio _usuarios;

    private readonly IValidator<UsuarioRequest> _validadorNovo;

    private readonly IValidator<UsuarioUpdateRequest> _validadorEdicao;

    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(IUsuarioNegocio usuarios, IValidator<UsuarioRequest> validadorNovo,
        IValidator<UsuarioUpdateRequest> validadorEdicao, ILogger<UsuariosController> logger)
    {
        _usuarios = usuarios;
        _validadorNovo = validadorNovo;
        _validadorEdicao = validadorEdicao;
        _logger = logger;
    }

    private async Task<IActionResult> ComErrosAsync(params string[] erros)
    {
        Vm.ErrosFormulario = erros.Select(Traduzir).Distinct().ToList();
        Vm.Usuario.Senha = null; // não devolve a senha digitada para a página
        await CarregarAsync();
        return View("Index", Vm);
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        Vm.Erro = mensagem;
        await CarregarAsync();
        return View("Index", Vm);
    }

    private async Task CarregarAsync()
    {
        // Ativos primeiro, depois por nome.
        Vm.Usuarios = (await _usuarios.GetAllAsync()).OrderByDescending(u => u.Ativo).ThenBy(u => u.Nome).ToList();
    }

    private static string Traduzir(string mensagem) => mensagem
        .Replace("e obrigatorio", "é obrigatório").Replace("invalido", "inválido")
        .Replace("numeros", "números").Replace("Login deve", "O login deve");

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost("Salvar")]
    public async Task<IActionResult> Salvar()
    {
        var novo = Vm.Usuario.Id == 0;
        if (!novo && Vm.Usuario.Id == Vm.MeuId)
        {
            // O próprio administrador não muda o próprio perfil aqui (evita se trancar fora);
            // os dados pessoais ele muda em "Meu perfil".
            var eu = await _usuarios.GetByIdAsync(Vm.MeuId);
            if (eu is not null && eu.Tipo != Vm.Usuario.Tipo)
                return await ComErrosAsync("Você não pode mudar o seu próprio perfil de acesso. Peça a outro administrador.");
        }

        try
        {
            if (novo)
            {
                var request = new UsuarioRequest
                {
                    Nome = Vm.Usuario.Nome.Trim(), Login = Vm.Usuario.Login.Trim(), Senha = Vm.Usuario.Senha ?? string.Empty,
                    Tipo = Vm.Usuario.Tipo, Email = Vm.Usuario.Email, Telefone = Vm.Usuario.Telefone
                };
                var validacao = await _validadorNovo.ValidateAsync(request);
                if (!validacao.IsValid) return await ComErrosAsync(validacao.Errors.Select(e => e.ErrorMessage).ToArray());

                var criado = await _usuarios.CreateAsync(request);
                _logger.LogInformation("[Painel] Usuário {Login} cadastrado por {Admin}.", criado.Login, User.Identity?.Name);
                TempData["Mensagem"] = $"Usuário {criado.Login} cadastrado como {criado.TipoDescricao}.";
            }
            else
            {
                var request = new UsuarioUpdateRequest
                {
                    Nome = Vm.Usuario.Nome.Trim(), Tipo = Vm.Usuario.Tipo, Email = Vm.Usuario.Email, Telefone = Vm.Usuario.Telefone,
                    Senha = string.IsNullOrEmpty(Vm.Usuario.Senha) ? null : Vm.Usuario.Senha
                };
                var validacao = await _validadorEdicao.ValidateAsync(request);
                if (!validacao.IsValid) return await ComErrosAsync(validacao.Errors.Select(e => e.ErrorMessage).ToArray());

                var atualizado = await _usuarios.UpdateAsync(Vm.Usuario.Id, request);
                if (atualizado is null) return await ComErrosAsync("Este usuário não existe mais.");
                TempData["Mensagem"] = request.Senha is null
                    ? $"Usuário {atualizado.Login} atualizado."
                    : $"Usuário {atualizado.Login} atualizado e com senha nova. Passe a senha a ele.";
            }
        }
        catch (InvalidOperationException ex)
        {
            return await ComErrosAsync(ex.Message);
        }
        return RedirectToAction("Index");
    }

    [HttpPost("Ativo")]
    public async Task<IActionResult> Ativo(int id, bool ativo)
    {
        if (id == Vm.MeuId) return await ComErroAsync("Você não pode desativar o seu próprio usuário.");
        var usuario = await _usuarios.GetByIdAsync(id);
        if (usuario is null) return await ComErroAsync("Este usuário não existe mais.");

        try
        {
            await _usuarios.UpdateAsync(id, new UsuarioUpdateRequest
            {
                Nome = usuario.Nome, Tipo = usuario.Tipo, Email = usuario.Email, Telefone = usuario.Telefone, Ativo = ativo
            });
        }
        catch (InvalidOperationException ex)
        {
            return await ComErroAsync(ex.Message);
        }
        _logger.LogInformation("[Painel] Usuário {Login} {Acao} por {Admin}.", usuario.Login, ativo ? "ativado" : "desativado", User.Identity?.Name);
        TempData["Mensagem"] = ativo
            ? $"Usuário {usuario.Login} ativado: já pode entrar no sistema."
            : $"Usuário {usuario.Login} desativado: não entra mais no sistema (o histórico dele continua).";
        return RedirectToAction("Index");
    }
}

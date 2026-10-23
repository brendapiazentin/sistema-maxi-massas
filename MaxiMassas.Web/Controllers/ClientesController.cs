// Clientes do painel: a lista é montada no servidor e o modal "+ NOVO"/editar grava pelo ClienteNegocio
// (o mesmo da API), com as mesmas validações do POST/PUT /api/Cliente.
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

namespace MaxiMassas.Web.Controllers;

[Route("Clientes")]
public class ClientesController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public ClientesViewModel Vm { get; set; } = new();

    private readonly IClienteNegocio _clientes;

    private readonly IVendaNegocio _vendas;

    private readonly IValidator<ClienteRequest> _validador;

    private readonly ILogger<ClientesController> _logger;

    public ClientesController(IClienteNegocio clientes, IVendaNegocio vendas, IValidator<ClienteRequest> validador,
        ILogger<ClientesController> logger)
    {
        _clientes = clientes;
        _vendas = vendas;
        _validador = validador;
        _logger = logger;
    }

    private async Task CarregarAsync()
    {
        var clientes = await _clientes.GetAllAsync();
        // Compras e total gasto: só vendas que valeram (canceladas não contam).
        var porCliente = (await _vendas.GetAllAsync())
            .Where(v => v.Status != StatusVenda.Cancelada)
            .GroupBy(v => v.ClienteId)
            .ToDictionary(g => g.Key, g => (Compras: g.Count(), Total: g.Sum(v => v.ValorTotal)));

        Vm.Linhas = clientes
            .OrderBy(c => c.Nome)
            .Select(c => porCliente.TryGetValue(c.Id, out var a)
                ? new ClienteLinha(c, a.Compras, a.Total)
                : new ClienteLinha(c, 0, 0))
            .ToList();
    }

    private static string? Vazio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>As mensagens do validador (compartilhado com a API) estão sem acento; aqui ficam legíveis.</summary>
    private static string TraduzirErro(string mensagem) => mensagem
        .Replace("e obrigatorio", "é obrigatório")
        .Replace("invalido", "inválido")
        .Replace("Numero", "Número")
        .Replace("numero", "número")
        .Replace("digitos", "dígitos");

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        var request = new ClienteUpdateRequest
        {
            // Campo vazio chega como null (padrão do ASP.NET): vira texto vazio e o validador acusa.
            Nome = (Vm.Cliente.Nome ?? string.Empty).Trim(),
            Telefone = (Vm.Cliente.Telefone ?? string.Empty).Trim(),
            CEP = (Vm.Cliente.Cep ?? string.Empty).Trim(),
            Numero = (Vm.Cliente.Numero ?? string.Empty).Trim(),
            Complemento = Vazio(Vm.Cliente.Complemento),
            Observacao = Vazio(Vm.Cliente.Observacao),
            Logradouro = Vazio(Vm.Cliente.Rua),
            Bairro = Vazio(Vm.Cliente.Bairro),
            Cidade = Vazio(Vm.Cliente.Cidade),
            Estado = Vazio(Vm.Cliente.Uf)?.ToUpper()
        };

        var validacao = await _validador.ValidateAsync(request);
        if (!validacao.IsValid)
        {
            Vm.ErrosFormulario = validacao.Errors.Select(e => TraduzirErro(e.ErrorMessage)).Distinct().ToList();
            await CarregarAsync();
            return View("Index", Vm);
        }

        if (Vm.Cliente.Id == 0)
        {
            var criado = await _clientes.CreateAsync(request);
            TempData["Mensagem"] = $"Cliente {criado.Nome} cadastrado.";
            _logger.LogInformation("[Painel] Cliente {Id} cadastrado.", criado.Id);
        }
        else
        {
            var atualizado = await _clientes.UpdateAsync(Vm.Cliente.Id, request);
            TempData["Mensagem"] = atualizado is null
                ? "Este cliente não existe mais."
                : $"Cadastro de {atualizado.Nome} atualizado.";
        }
        return RedirectToAction("Index");
    }
}

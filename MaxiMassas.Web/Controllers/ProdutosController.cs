// Produtos do painel: lista montada no servidor; o modal cadastra/altera pelo ProdutoNegocio (o mesmo da API).
// No cadastro, o "Estoque" informado vira o estoque inicial (com movimentação de entrada registrada);
// na alteração ele só é exibido — mudar estoque é na tela de Estoque, que registra o motivo.
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

[Route("Produtos")]
public class ProdutosController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public ProdutosViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.PodeEditar = User.IsInRole(nameof(TipoUsuario.Administrador));
        base.OnActionExecuting(context);
    }

    private readonly IProdutoNegocio _produtos;

    private readonly IEstoqueNegocio _estoque;

    private readonly IValidator<ProdutoRequest> _validador;

    private readonly ILogger<ProdutosController> _logger;
    private readonly IFotoProdutoService _fotos;

    public ProdutosController(IProdutoNegocio produtos, IEstoqueNegocio estoque, IValidator<ProdutoRequest> validador,
        ILogger<ProdutosController> logger, IFotoProdutoService fotos)
    {
        _produtos = produtos;
        _estoque = estoque;
        _validador = validador;
        _logger = logger;
        _fotos = fotos;
    }

    private async Task CarregarAsync()
    {
        Vm.Produtos = (await _produtos.GetAllAsync())
            .OrderBy(p => p.Nome).ThenBy(p => p.Sabor)
            .ToList();
    }

    /// <summary>Grava ou remove a foto depois que o produto foi salvo.</summary>
    private async Task AplicarFotoAsync(int produtoId, byte[]? foto, bool remover)
    {
        if (foto is not null)
        {
            await _fotos.SalvarAsync(produtoId, foto);
            await _produtos.DefinirFotoAsync(produtoId, DateTime.Now);
        }
        else if (remover)
        {
            await _fotos.ExcluirAsync(produtoId);
            await _produtos.DefinirFotoAsync(produtoId, null);
        }
    }

    private static string Traduzir(string mensagem) => mensagem
        .Replace("e obrigatorio", "é obrigatório")
        .Replace("Preco de venda", "Preço de venda")
        .Replace("nao pode", "não pode")
        .Replace("invalido", "inválido");

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        if (!Vm.PodeEditar)
        {
            Vm.ErrosFormulario = new[] { "Somente administradores podem cadastrar ou alterar produtos." };
            await CarregarAsync();
            return View("Index", Vm);
        }

        var erros = new List<string>();
        var peso = PainelFormatos.LerPeso(Vm.Produto.Peso);
        var custo = PainelFormatos.LerMoeda(Vm.Produto.Custo);
        var preco = PainelFormatos.LerMoeda(Vm.Produto.Preco);
        if (peso is null) erros.Add("Informe o peso (ex.: 250g).");
        if (custo is null) erros.Add("Informe o custo (ex.: R$ 18,90).");
        if (preco is null) erros.Add("Informe o preço (ex.: R$ 39,90).");
        if (!Enum.TryParse<TipoProduto>(Vm.Produto.Tipo, out var tipo) || !Enum.IsDefined(tipo)) erros.Add("Escolha o tipo do produto.");
        var novo = Vm.Produto.Id == 0;
        // Foto (opcional): confere antes de salvar o produto, para o erro aparecer no formulario.
        byte[]? fotoPronta = null;
        if (Vm.Produto.Foto is { Length: > 0 } arquivoFoto)
        {
            try
            {
                using var fluxoFoto = arquivoFoto.OpenReadStream();
                fotoPronta = await _fotos.PrepararAsync(fluxoFoto, arquivoFoto.Length);
            }
            catch (InvalidOperationException ex) { erros.Add(ex.Message); }
        }
        if (novo && (Vm.Produto.Estoque is null || Vm.Produto.Estoque < 0)) erros.Add("Informe o estoque inicial (0 ou mais).");

        var request = new ProdutoUpdateRequest
        {
            Nome = (Vm.Produto.Nome ?? string.Empty).Trim(), // campo vazio chega como null
            Sabor = Vm.Produto.Sabor,
            Observacao = Vm.Produto.Observacao,
            Peso = peso ?? 0,
            Custo = custo ?? 0,
            PrecoVenda = preco ?? 0,
            Tipo = tipo,
            Ativo = Vm.Produto.Ativo,
            MotivoAlteracao = "Alterado no painel"
        };
        if (erros.Count == 0)
        {
            var validacao = await _validador.ValidateAsync(request);
            erros.AddRange(validacao.Errors.Select(e => Traduzir(e.ErrorMessage)));
        }
        if (erros.Count > 0)
        {
            Vm.ErrosFormulario = erros.Distinct().ToList();
            await CarregarAsync();
            return View("Index", Vm);
        }

        if (novo)
        {
            var criado = await _produtos.CreateAsync(request);
            var estoque = await _estoque.GarantirEstoqueAsync(criado.Id, PainelFormatos.AlertaMinimoPadrao);
            if (Vm.Produto.Estoque > 0)
            {
                await _estoque.RegistrarMovimentacaoAsync(new MovimentacaoEstoqueRequest
                {
                    EstoqueId = estoque.Id, Tipo = TipoMovimentacao.Entrada, Quantidade = Vm.Produto.Estoque.Value,
                    Motivo = "Estoque inicial (cadastro do produto)"
                });
            }
            _logger.LogInformation("[Painel] Produto {Id} cadastrado com estoque {Qtd}.", criado.Id, Vm.Produto.Estoque);
            await AplicarFotoAsync(criado.Id, fotoPronta, false);
            TempData["Mensagem"] = $"Produto {criado.Nome} cadastrado.";
        }
        else
        {
            var atualizado = await _produtos.UpdateAsync(Vm.Produto.Id, request);
            if (atualizado is not null) await AplicarFotoAsync(atualizado.Id, fotoPronta, Vm.Produto.RemoverFoto);
            TempData["Mensagem"] = atualizado is null ? "Este produto não existe mais." : $"Produto {atualizado.Nome} atualizado.";
        }
        return RedirectToAction("Index");
    }
}

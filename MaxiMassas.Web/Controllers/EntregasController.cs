// Entregas do painel: lista de vendas com entrega (retiradas ficam de fora), montada no servidor,
// e o modal "Pedido mínimo e taxa de entrega", gravado pelo ConfiguracaoEntregaNegocio (só Administrador).
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("Entregas")]
public class EntregasController : PainelController
{
    /// <summary>Estado da tela (o ViewModel). O nome vazio faz o binding usar os mesmos nomes de campo das Razor Pages.</summary>
    [BindProperty(Name = "", SupportsGet = true)] public EntregasViewModel Vm { get; set; } = new();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.PodeAlterarTaxa = User.IsInRole(nameof(TipoUsuario.Administrador));
        base.OnActionExecuting(context);
    }

    private readonly IVendaNegocio _vendas;

    private readonly IClienteNegocio _clientes;

    private readonly IConfiguracaoEntregaNegocio _config;

    private readonly ILogger<EntregasController> _logger;

    public EntregasController(IVendaNegocio vendas, IClienteNegocio clientes, IConfiguracaoEntregaNegocio config,
        ILogger<EntregasController> logger)
    {
        _vendas = vendas;
        _clientes = clientes;
        _config = config;
        _logger = logger;
    }

    private async Task<IActionResult> ComErroAsync(string mensagem)
    {
        Vm.ErroTaxa = mensagem;
        await CarregarAsync();
        return View("Index", Vm);
    }

    private async Task CarregarAsync()
    {
        var clientes = (await _clientes.GetAllAsync()).ToDictionary(c => c.Id);
        Vm.Linhas = (await _vendas.GetAllAsync())
            // Retirada no local não é entrega (pedidos do app com retirada também têm Entrega)
            .Where(v => v.Entrega is not null && v.Entrega.TipoEntrega != TipoEntrega.Retirada)
            .OrderByDescending(v => v.Entrega!.DataEntrega).ThenByDescending(v => v.Data)
            .Select(v => new LinhaEntrega(v, Endereco(v, clientes.GetValueOrDefault(v.ClienteId))))
            .ToList();
        Vm.Anos = Vm.Linhas.Select(l => l.Venda.Entrega!.DataEntrega.Year).Append(DateTime.Today.Year)
            .Distinct().OrderByDescending(a => a).ToList();
        Vm.Taxa = await _config.ObterResponseAsync();
    }

    private static EnderecoEntrega Endereco(VendaResponse v, ClienteResponse? c)
    {
        var e = v.Entrega!;
        return string.IsNullOrEmpty(e.Logradouro)
            ? new EnderecoEntrega(c?.Logradouro ?? "", c?.Numero ?? "", c?.Bairro ?? "", c?.Complemento ?? "", c?.Cidade ?? "")
            : new EnderecoEntrega(e.Logradouro, e.Numero ?? "", e.Bairro ?? "", e.Complemento ?? "", e.Cidade ?? c?.Cidade ?? "");
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarAsync();
    return View(Vm);
    }

    [HttpPost("Taxa")]
    public async Task<IActionResult> Taxa(string? taxaEntrega, string? pedidoMinimo)
    {
        Vm.TaxaDigitada = taxaEntrega;
        Vm.MinimoDigitado = pedidoMinimo;

        if (!Vm.PodeAlterarTaxa) return await ComErroAsync("Somente administradores podem alterar a taxa de entrega.");

        var taxa = PainelFormatos.LerMoeda(taxaEntrega);
        var minimo = PainelFormatos.LerMoeda(pedidoMinimo);
        if (taxa is null or < 0 || minimo is null or < 0)
            return await ComErroAsync("Informe os dois valores em reais, por exemplo R$ 4,99 (use R$ 0,00 para nenhum).");

        await _config.AtualizarAsync(new ConfiguracaoEntregaRequest { TaxaEntrega = taxa.Value, PedidoMinimoEntregaGratis = minimo.Value });
        _logger.LogInformation("[Painel] Taxa de entrega: {Taxa}, grátis a partir de {Minimo}.", taxa, minimo);
        TempData["Mensagem"] = minimo > 0
            ? $"Taxa de entrega: {PainelFormatos.Moeda(taxa.Value)}. Grátis em pedidos a partir de {PainelFormatos.Moeda(minimo.Value)}."
            : $"Taxa de entrega: {PainelFormatos.Moeda(taxa.Value)} em todos os pedidos.";
        return RedirectToAction("Index");
    }
}

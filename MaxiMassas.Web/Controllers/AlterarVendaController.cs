// Editar venda: mesma tela do PDV, mas só status, forma de pagamento, plataforma e observação mudam.
// Itens, cliente e entrega ficam travados — o estoque já foi baixado por eles. Para trocar itens,
// cancela-se a venda (os itens voltam ao estoque) e registra-se outra.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("AlterarVenda/{id:int}")]
public class AlterarVendaController : PdvController
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.Titulo = "Editar Venda";
        base.OnActionExecuting(context);
    }

    private readonly ILogger<AlterarVendaController> _logger;

    public AlterarVendaController(IVendaNegocio vendas, IClienteNegocio clientes, IProdutoNegocio produtos,
        IConfiguracaoEntregaNegocio configEntrega, ILogger<AlterarVendaController> logger)
        : base(vendas, clientes, produtos, configEntrega)
    {
        _logger = logger;
    }

    private IActionResult VendaNaoEncontrada()
    {
        TempData["Mensagem"] = $"Venda #{Vm.Id} não encontrada.";
        return RedirectToAction("Index", "Vendas");
    }

    /// <summary>A comanda montada a partir da venda gravada (com as escolhas de [form] por cima, se houver).</summary>
    private async Task<object> EstadoDaVendaAsync(VendaResponse v, VendaForm? form = null)
    {
        var cliente = await Clientes.GetByIdAsync(v.ClienteId);
        var entrega = v.Entrega;
        var delivery = entrega?.TipoEntrega == TipoEntrega.Entrega;

        return new
        {
            cliente = cliente is null
                ? new { id = v.ClienteId, nome = v.NomeCliente, tel = "", rua = "", num = "", bairro = "", cid = "", uf = "", comp = "" }
                : ClienteParaTela(cliente),
            itens = v.Itens.Select(i => new { idProduto = i.ProdutoId, qtd = i.Quantidade }),
            tipoDesconto = v.TipoDesconto == TipoDesconto.Percentual ? "PCT" : "RS",
            valorDesconto = v.Desconto,
            // A taxa cobrada na época fica registrada; não é recalculada pela regra atual.
            taxaEntrega = v.Frete,
            tipoEntrega = delivery ? "Delivery" : "Retirada",
            plataforma = form?.Plataforma ?? v.Origem,
            pagamento = form?.Pagamento ?? RotuloPagamento(v.Pagamento?.FormaPagamento),
            status = form?.Status ?? RotuloStatus(v.Status),
            obs = form?.Observacao ?? v.Observacao ?? string.Empty,
            dataAgendada = entrega is null ? "" : entrega.DataEntrega.ToString("dd/MM/yyyy"),
            horaAgendada = entrega?.Horario ?? "",
            endereco = delivery
                ? new
                {
                    rua = entrega!.Logradouro ?? cliente?.Logradouro ?? "",
                    num = entrega.Numero ?? cliente?.Numero ?? "",
                    bairro = entrega.Bairro ?? cliente?.Bairro ?? "",
                    cid = entrega.Cidade ?? cliente?.Cidade ?? "",
                    uf = entrega.Estado ?? cliente?.Estado ?? "",
                    comp = entrega.Complemento ?? cliente?.Complemento ?? ""
                }
                : null,
            dataVenda = v.Data.ToString("dd/MM/yyyy HH:mm")
        };
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var venda = await Vendas.GetByIdAsync(Vm.Id);
        if (venda is null) return VendaNaoEncontrada();

        await CarregarDadosAsync(await EstadoDaVendaAsync(venda), modo: "edicao", vendaId: venda.Id);
        return View("Index", Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        var venda = await Vendas.GetByIdAsync(Vm.Id);
        if (venda is null) return VendaNaoEncontrada();

        if (!StatusPorRotulo.TryGetValue(Vm.Venda.Status, out var status)) Vm.Erro = "Escolha o status da venda.";
        else if (!PagamentoPorRotulo.TryGetValue(Vm.Venda.Pagamento, out _)) Vm.Erro = "Escolha a forma de pagamento.";
        else if (string.IsNullOrWhiteSpace(Vm.Venda.Plataforma)) Vm.Erro = "Escolha a plataforma do pedido.";
        else if ((Vm.Venda.Observacao?.Length ?? 0) > 500) Vm.Erro = "A observação pode ter no máximo 500 caracteres.";

        if (Vm.Erro is null)
        {
            try
            {
                await Vendas.UpdateAsync(Vm.Id, new VendaUpdateRequest
                {
                    Status = status.Venda,
                    StatusPagamento = status.Pagamento,
                    // Mantém a data de um pagamento que já tinha sido feito.
                    DataPagamento = venda.Pagamento?.DataPagamento,
                    Origem = Vm.Venda.Plataforma.Trim(),
                    FormaPagamento = PagamentoPorRotulo[Vm.Venda.Pagamento],
                    Observacao = Vm.Venda.Observacao ?? string.Empty
                });
                _logger.LogInformation("[Painel] Venda #{VendaId} alterada para {Status}.", Vm.Id, status.Venda);
                TempData["Mensagem"] = status.Venda == StatusVenda.Cancelada && venda.Status != StatusVenda.Cancelada
                    ? $"Venda #{Vm.Id} cancelada. Os itens voltaram ao estoque."
                    : $"Venda #{Vm.Id} atualizada.";
                return RedirectToAction("Index", "Vendas");
            }
            catch (InvalidOperationException ex)
            {
                Vm.Erro = ex.Message; // ex.: tentar reabrir uma venda cancelada
            }
        }

        // Devolve a tela com a venda como está no banco, mantendo as escolhas editáveis do formulário.
        var estado = await EstadoDaVendaAsync(venda, Vm.Venda);
        await CarregarDadosAsync(estado, modo: "edicao", vendaId: venda.Id);
        return View("Index", Vm);
    }
}

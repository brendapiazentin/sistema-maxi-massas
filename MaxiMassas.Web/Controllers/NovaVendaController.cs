// Nova venda (PDV do balcão): grava pelo VendaNegocio — valida e baixa o estoque numa transação.
using MaxiMassas.Modelos.Dtos.Request;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("NovaVenda")]
public class NovaVendaController : PdvController
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Vm.Titulo = "Nova Venda";
        base.OnActionExecuting(context);
    }

    private readonly ILogger<NovaVendaController> _logger;

    public NovaVendaController(IVendaNegocio vendas, IClienteNegocio clientes, IProdutoNegocio produtos,
        IConfiguracaoEntregaNegocio configEntrega, ILogger<NovaVendaController> logger)
        : base(vendas, clientes, produtos, configEntrega)
    {
        _logger = logger;
    }

    /// <summary>Confere o formulário; devolve a mensagem do primeiro problema ou null.</summary>
    private string? Validar(out List<ItemForm> itens, out (StatusVenda Venda, StatusPagamento Pagamento) status,
        out FormaPagamento pagamento, out DateTime? dataAgendada)
    {
        itens = LerItens();
        status = default;
        pagamento = default;
        dataAgendada = LerData(Vm.Venda.DataAgendada);

        if (Vm.Venda.ClienteId <= 0) return "Selecione o cliente da venda.";
        if (itens.Count == 0) return "Adicione pelo menos um produto à comanda.";
        if (Vm.Venda.TipoEntrega is not ("Retirada" or "Delivery")) return "Escolha o tipo de entrega (Retirada ou Delivery).";
        if (Vm.Venda.TipoEntrega == "Delivery" &&
            (string.IsNullOrWhiteSpace(Vm.Venda.Rua) || string.IsNullOrWhiteSpace(Vm.Venda.Numero) || string.IsNullOrWhiteSpace(Vm.Venda.Bairro)))
            return "Para delivery, preencha rua, número e bairro do endereço de entrega.";
        if (Vm.Venda.TipoEntrega == "Delivery" && (Vm.Venda.Uf?.Trim().Length ?? 0) > 2) return "UF deve ter 2 letras (ex.: SP).";
        if (string.IsNullOrWhiteSpace(Vm.Venda.Plataforma)) return "Escolha a plataforma do pedido.";
        if (!PagamentoPorRotulo.TryGetValue(Vm.Venda.Pagamento, out pagamento)) return "Escolha a forma de pagamento.";
        if (!StatusPorRotulo.TryGetValue(Vm.Venda.Status, out status)) return "Escolha o status da venda.";
        if (status.Venda == StatusVenda.Cancelada) return "Uma venda nova não pode ser registrada como cancelada.";
        if (!string.IsNullOrWhiteSpace(Vm.Venda.DataAgendada) && dataAgendada is null) return "Data agendada inválida. Use DD/MM/AAAA.";
        if (!string.IsNullOrWhiteSpace(Vm.Venda.HoraAgendada) && LerHora(Vm.Venda.HoraAgendada) is null) return "Hora agendada inválida. Use HH:MM.";
        if (Vm.Venda.Status == "Agendado" && dataAgendada is null) return "Para agendar, informe a data agendada.";
        if ((Vm.Venda.Observacao?.Length ?? 0) > 500) return "A observação pode ter no máximo 500 caracteres.";
        return null;
    }

    private async Task<VendaRequest> MontarRequestAsync(List<ItemForm> itens, (StatusVenda Venda, StatusPagamento Pagamento) status,
        FormaPagamento pagamento, DateTime? dataAgendada)
    {
        var delivery = Vm.Venda.TipoEntrega == "Delivery";

        // Taxa definida pelos gestores (Entregas → Pedido mínimo e taxa), sobre o subtotal dos itens.
        decimal taxa = 0;
        if (delivery)
        {
            decimal subtotal = 0;
            foreach (var item in itens)
            {
                var produto = await Produtos.GetByIdAsync(item.ProdutoId)
                    ?? throw new KeyNotFoundException($"Produto ID {item.ProdutoId} não encontrado.");
                subtotal += produto.PrecoVenda * item.Quantidade;
            }
            taxa = (await ConfigEntrega.ObterAsync()).CalcularTaxa(subtotal);
        }

        return new VendaRequest
        {
            ClienteId = Vm.Venda.ClienteId,
            Itens = itens.GroupBy(i => i.ProdutoId)
                .Select(g => new ItemVendaRequest { ProdutoId = g.Key, Quantidade = g.Sum(i => i.Quantidade) }).ToList(),
            Desconto = LerDesconto(),
            TipoDesconto = LerDesconto() == 0 ? TipoDesconto.Nenhum
                : Vm.Venda.TipoDesconto == "PCT" ? TipoDesconto.Percentual : TipoDesconto.Valor,
            Frete = taxa,
            Observacao = string.IsNullOrWhiteSpace(Vm.Venda.Observacao) ? null : Vm.Venda.Observacao.Trim(),
            Origem = Vm.Venda.Plataforma.Trim(),
            Status = status.Venda,
            Pagamento = new PagamentoRequest { FormaPagamento = pagamento, StatusPagamento = status.Pagamento },
            Entrega = new EntregaRequest
            {
                TipoEntrega = delivery ? TipoEntrega.Entrega : TipoEntrega.Retirada,
                DataEntrega = dataAgendada ?? DateTime.Today,
                Horario = LerHora(Vm.Venda.HoraAgendada) ?? string.Empty,
                ValorEntrega = taxa,
                Logradouro = delivery ? Vm.Venda.Rua?.Trim() : null,
                Numero = delivery ? Vm.Venda.Numero?.Trim() : null,
                Complemento = delivery && !string.IsNullOrWhiteSpace(Vm.Venda.Complemento) ? Vm.Venda.Complemento.Trim() : null,
                Bairro = delivery ? Vm.Venda.Bairro?.Trim() : null,
                Cidade = delivery && !string.IsNullOrWhiteSpace(Vm.Venda.Cidade) ? Vm.Venda.Cidade.Trim() : null,
                Estado = delivery && !string.IsNullOrWhiteSpace(Vm.Venda.Uf) ? Vm.Venda.Uf.Trim().ToUpper() : null
            }
        };
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
    await CarregarDadosAsync(estado: null, modo: "nova");
    return View(Vm);
    }

    [HttpPost(""), ActionName("Index")]
    public async Task<IActionResult> IndexPost()
    {
        Vm.Erro = Validar(out var itens, out var status, out var pagamento, out var dataAgendada);
        if (Vm.Erro is null)
        {
            try
            {
                var criada = await Vendas.CreateAsync(await MontarRequestAsync(itens, status, pagamento, dataAgendada), UsuarioId);
                _logger.LogInformation("[Painel] Venda #{VendaId} registrada no PDV.", criada.Id);
                TempData["Mensagem"] = $"Venda #{criada.Id} registrada para {criada.NomeCliente}.";
                return RedirectToAction("Index", "Vendas");
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
            {
                // Ex.: estoque insuficiente (inclusive quando outra venda levou as últimas unidades agora).
                Vm.Erro = ex.Message;
            }
        }

        await CarregarDadosAsync(await EstadoDoFormularioAsync(), modo: "nova");
        return View("Index", Vm);
    }
}

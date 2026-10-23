// Aviso de pedido novo do app: o painel (wwwroot/js/pedidos-app.js) consulta este endereço a cada poucos
// segundos e recebe, em JSON, os pedidos do app ainda "Pendentes" (ninguém mudou o status ainda).
// Quando aparece um id novo, o painel toca um som, mostra um aviso e atualiza o contador no menu Vendas.
// O pedido sai da lista assim que alguém o atende (Agendado, Pago, Concluído ou Cancelado).
// Exige o login do painel como qualquer outra tela.
using MaxiMassas.Negocio;
using MaxiMassas.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace MaxiMassas.Web.Controllers;

[Route("PedidosApp")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PedidosAppController : PainelController
{
    private readonly IVendaNegocio _vendas;

    public PedidosAppController(IVendaNegocio vendas) { _vendas = vendas; }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var pendentes = await _vendas.GetPedidosAppPendentesAsync();

        return new JsonResult(new
        {
            total = pendentes.Total,
            pedidos = pendentes.Pedidos.Select(p => new
            {
                id = p.Id,
                cliente = p.Cliente,
                valor = PainelFormatos.Moeda(p.ValorTotal),
                hora = p.Data.ToString("HH:mm"),
                tipo = p.Entrega ? "Entrega" : "Retirada",
                url = Url.Action("Index", "AlterarVenda", new { id = p.Id })
            })
        });
    }
}

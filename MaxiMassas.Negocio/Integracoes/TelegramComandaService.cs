// Envia a comanda de uma venda com entrega para o Telegram dos entregadores (bot da loja).
// O token do bot vem da configuracao "Telegram:BotToken" (variavel de ambiente Telegram__BotToken no servidor)
// e os destinatarios (chat_id) vem da tela Entregas > Configuracoes de entrega.
// Falha no envio nunca derruba a venda: o erro e registrado no log (sem o token) e o fluxo segue.
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using MaxiMassas.Modelos;

namespace MaxiMassas.Negocio.Integracoes;

public interface ITelegramComandaService
{
    /// <summary>Monta a comanda da venda (com cliente, itens, pagamento e entrega carregados) e envia aos entregadores.</summary>
    Task EnviarComandaAsync(Venda venda);
}

public class TelegramComandaService : ITelegramComandaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguracaoEntregaNegocio _config;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramComandaService> _logger;

    public TelegramComandaService(IHttpClientFactory httpClientFactory, IConfiguracaoEntregaNegocio config,
        IConfiguration configuration, ILogger<TelegramComandaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarComandaAsync(Venda venda)
    {
        try
        {
            var token = _configuration["Telegram:BotToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("[Telegram] Token do bot nao configurado: comanda da venda #{VendaId} nao enviada.", venda.Id);
                return;
            }

            var cfg = await _config.ObterAsync();
            var destinos = new[] { cfg.TelegramEntregador1, cfg.TelegramEntregador2 }
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Select(d => d!.Trim())
                .Distinct()
                .ToList();
            if (destinos.Count == 0)
            {
                _logger.LogInformation("[Telegram] Nenhum entregador cadastrado: comanda da venda #{VendaId} nao enviada.", venda.Id);
                return;
            }

            var enderecoTexto = MontarEndereco(venda);
            var consulta = WebUtility.UrlEncode(enderecoTexto + ", Brasil").Replace("+", "%20");
            var linkWaze = $"https://waze.com/ul?q={consulta}&navigate=yes";
            var linkMaps = $"https://www.google.com/maps/search/?api=1&query={consulta}";
            var texto = MontarTexto(venda, enderecoTexto);

            await Task.WhenAll(destinos.Select(d => EnviarAsync(token, d, texto, linkWaze, linkMaps, venda.Id)));
        }
        catch (Exception ex)
        {
            // Nao registra ex.Message por precaucao: nenhuma informacao do token deve ir para o log.
            _logger.LogError("[Telegram] Falha ao preparar a comanda da venda #{VendaId}: {Tipo}", venda.Id, ex.GetType().Name);
        }
    }

    private async Task EnviarAsync(string token, string chatId, string texto, string linkWaze, string linkMaps, int vendaId)
    {
        try
        {
            var payload = new
            {
                chat_id = chatId,
                text = texto,
                parse_mode = "HTML",
                disable_web_page_preview = true,
                reply_markup = new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "Abrir no Waze", url = linkWaze },
                            new { text = "Abrir no Google Maps", url = linkMaps }
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient("Telegram");
            var resposta = await client.PostAsJsonAsync($"bot{token}/sendMessage", payload);
            if (resposta.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Telegram] Comanda da venda #{VendaId} enviada (chat {ChatId}).", vendaId, chatId);
                return;
            }

            // O corpo do erro do Telegram traz so a descricao (ex.: "chat not found"), sem o token.
            var corpo = await resposta.Content.ReadAsStringAsync();
            _logger.LogWarning("[Telegram] Telegram recusou a comanda da venda #{VendaId} (chat {ChatId}): {Status} {Corpo}",
                vendaId, chatId, (int)resposta.StatusCode, corpo);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Telegram] Falha ao enviar a comanda da venda #{VendaId} (chat {ChatId}): {Tipo}",
                vendaId, chatId, ex.GetType().Name);
        }
    }

    /// <summary>Endereco da entrega; se a venda nao trouxer um, usa o do cadastro do cliente.</summary>
    private static string MontarEndereco(Venda v)
    {
        var e = v.Entrega;
        var c = v.Cliente;
        var temProprio = !string.IsNullOrWhiteSpace(e?.Logradouro);
        var logradouro = temProprio ? e!.Logradouro : c?.Logradouro;
        var numero = temProprio ? e!.Numero : c?.Numero;
        var complemento = temProprio ? e!.Complemento : c?.Complemento;
        var bairro = temProprio ? e!.Bairro : c?.Bairro;
        var cidade = temProprio ? (e!.Cidade ?? c?.Cidade) : c?.Cidade;
        var estado = temProprio ? (e!.Estado ?? c?.Estado) : c?.Estado;
        var cep = temProprio ? e!.Cep : c?.CEP;

        var partes = new List<string>();
        var rua = string.Join(", ", new[] { logradouro, numero }.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (rua.Length > 0) partes.Add(rua);
        if (!string.IsNullOrWhiteSpace(complemento)) partes.Add(complemento!);
        if (!string.IsNullOrWhiteSpace(bairro)) partes.Add(bairro!);
        var cidadeUf = string.Join(" - ", new[] { cidade, estado }.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (cidadeUf.Length > 0) partes.Add(cidadeUf);
        if (!string.IsNullOrWhiteSpace(cep)) partes.Add("CEP " + cep);
        return string.Join(", ", partes);
    }

    private static string MontarTexto(Venda v, string endereco)
    {
        static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        static string R(decimal valor) => "R$ " + valor.ToString("F2", CultureInfo.InvariantCulture).Replace('.', ',');

        var linhas = new List<string>
        {
            $"🛵 <b>NOVA ENTREGA — Pedido #{v.Id}</b>",
            "",
            $"👤 <b>Cliente:</b> {H(v.Cliente?.Nome)}",
            $"📞 <b>Celular:</b> {H(v.Cliente?.Telefone)}"
        };

        if (!string.IsNullOrWhiteSpace(v.Entrega?.Horario))
            linhas.Add($"🕒 <b>Horário:</b> {H(v.Entrega!.Horario)}");

        linhas.Add($"📍 <b>Endereço:</b> {H(endereco)}");
        linhas.Add("");
        linhas.Add("🧾 <b>Itens:</b>");
        foreach (var i in v.ItensVenda)
            linhas.Add($"• {i.Quantidade}x {H(i.Produto?.Nome)} — {R(i.Subtotal)}");

        linhas.Add("");
        if (v.Frete > 0) linhas.Add($"🚚 Taxa de entrega: {R(v.Frete)}");
        else if ((v.Entrega?.ValorEntrega ?? 0) > 0) linhas.Add($"🚚 Taxa de entrega: {R(v.Entrega!.ValorEntrega)}");
        else linhas.Add("🚚 Entrega grátis");
        if (v.Desconto > 0) linhas.Add("🏷️ Desconto aplicado");
        linhas.Add($"💰 <b>Total: {R(v.ValorTotal)}</b>");
        if (v.Pagamento is not null)
            linhas.Add($"💳 <b>Pagamento:</b> {NomeForma(v.Pagamento.FormaPagamento)}");
        if (!string.IsNullOrWhiteSpace(v.Observacao))
            linhas.Add($"📝 <b>Obs.:</b> {H(v.Observacao)}");

        return string.Join("\n", linhas);
    }

    private static string NomeForma(FormaPagamento f) => f switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.CartaoDebito => "Cartão de débito",
        FormaPagamento.CartaoCredito => "Cartão de crédito",
        FormaPagamento.Pix => "Pix",
        FormaPagamento.Transferencia => "Transferência",
        _ => "Outro"
    };
}

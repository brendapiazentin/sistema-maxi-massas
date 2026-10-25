namespace MaxiMassas.Modelos.Dtos.Response;

public class ConfiguracaoEntregaResponse
{
    public decimal TaxaEntrega { get; set; }
    public decimal PedidoMinimoEntregaGratis { get; set; }
    public string? TelegramEntregador1 { get; set; }
    public string? TelegramEntregador2 { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

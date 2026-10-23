namespace MaxiMassas.Modelos.Dtos.Response;

/// <summary>Um pedido do app que ainda espera atendimento (aviso do painel).</summary>
public class PedidoAppPendenteResponse
{
    public int Id { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public DateTime Data { get; set; }
    /// <summary>true = entrega; false = retirada.</summary>
    public bool Entrega { get; set; }
}

/// <summary>Pedidos pendentes do app: os mais recentes e o total (a lista é limitada).</summary>
public class PedidosAppPendentesResponse
{
    public int Total { get; set; }
    public List<PedidoAppPendenteResponse> Pedidos { get; set; } = new();
}

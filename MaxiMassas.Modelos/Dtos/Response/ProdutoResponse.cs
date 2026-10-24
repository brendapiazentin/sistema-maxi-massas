namespace MaxiMassas.Modelos.Dtos.Response;

using MaxiMassas.Modelos;

public class ProdutoResponse
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Sabor { get; set; }
    public string? Observacao { get; set; }
    public decimal Peso { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Custo { get; set; }
    public TipoProduto Tipo { get; set; }
    public bool Ativo { get; set; }
    public DateTime? FotoAtualizadaEm { get; set; }
    public EstoqueResponse? Estoque { get; set; }
}
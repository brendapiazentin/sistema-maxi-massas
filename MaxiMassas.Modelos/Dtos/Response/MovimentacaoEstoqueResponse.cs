namespace MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;

public class MovimentacaoEstoqueResponse
{
    public int Id { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public string TipoDescricao => Tipo.ToString();
    public string Motivo { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public DateTime Data { get; set; }
}
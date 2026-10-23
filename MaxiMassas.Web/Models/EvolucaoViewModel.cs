// ViewModel da tela "Evolucao": o que a view mostra e o que o formulario envia.
using MaxiMassas.Modelos.Dtos.Response;
using MaxiMassas.Modelos;
using MaxiMassas.Negocio;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MaxiMassas.Web.Models;

public class EvolucaoViewModel
{
    [BindNever] public bool VeFinanceiro { get; set; }

    /// <summary>{ "Mês": { labels, receita, lucro, produtos: [{ nome, faturado, quantidade }] }, ... }</summary>
    [BindNever] public string DadosJson { get; set; } = "{}";

    // ── Exportar relatório (US14): Excel e PDF do período escolhido, só para Administrador ──

    /// <summary>Período padrão do formulário: do dia 1 do mês até hoje.</summary>
    public DateTime ExportarInicio => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public DateTime ExportarFim => DateTime.Today;

    /// <summary>Erro de período (ex.: início depois do fim); a página volta com a mensagem.</summary>
    [BindNever] public string? ErroExportacao { get; set; }
}

// HorarioFuncionamento — janelas de atendimento da loja, usadas pelo app do cliente.
//
// Regra informada pelos proprietários (atualizada em 07/11/2026):
//   Segunda a sexta: retirada 10h–19h; entregas 18h–19h30.
//   Sábado:          retirada 10h–19h; entregas 13h–19h30.
//   Domingo:         retirada e entregas 10h–13h.
//
// A mesma classe gera os horários oferecidos ao app (GET /api/app/horarios) e valida o
// horário recebido no pedido (POST /api/app/pedidos). Assim a regra fica num único lugar:
// o app não precisa conhecer a tabela e não consegue enviar um horário fora da janela.
using System.Globalization;
using MaxiMassas.Modelos;
using MaxiMassas.Modelos.Dtos.Response;

namespace MaxiMassas.Negocio;

public static class HorarioFuncionamento
{
    /// <summary>Formato do horário trocado entre app e API, ex.: "03/11/2026 18:30".</summary>
    public const string Formato = "dd/MM/yyyy HH:mm";

    /// <summary>Intervalo entre os horários oferecidos ao cliente.</summary>
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    /// <summary>Tempo mínimo entre o pedido e o horário escolhido (separar e embalar o pedido).</summary>
    public static readonly TimeSpan Antecedencia = TimeSpan.FromMinutes(30);

    /// <summary>Quantos dias à frente o cliente pode agendar (hoje + 2).</summary>
    public const int DiasParaAgendar = 3;

    private static readonly string[] DiasSemana = { "Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb" };

    /// <summary>Janela de atendimento do dia: o último horário oferecido é anterior ao fim.</summary>
    public static (TimeSpan Inicio, TimeSpan Fim) Janela(DayOfWeek dia, TipoEntrega tipo) => dia switch
    {
        DayOfWeek.Sunday => (Hora(10), Hora(13)),
        DayOfWeek.Saturday => tipo == TipoEntrega.Entrega ? (Hora(13), Hora(19, 30)) : (Hora(10), Hora(19)),
        _ => tipo == TipoEntrega.Entrega ? (Hora(18), Hora(19, 30)) : (Hora(10), Hora(19))
    };

    /// <summary>Hora oficial de Brasília, independente do fuso configurado no servidor.</summary>
    public static DateTime Agora()
    {
        try
        {
            var fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, fuso);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.Now;
        }
    }

    /// <summary>Horários que o cliente pode escolher, a partir de agora, nos próximos dias.</summary>
    public static List<HorarioAppResponse> Disponiveis(TipoEntrega tipo, DateTime agora)
    {
        var lista = new List<HorarioAppResponse>();
        var minimo = agora + Antecedencia;

        for (var d = 0; d < DiasParaAgendar; d++)
        {
            var dia = agora.Date.AddDays(d);
            var (inicio, fim) = Janela(dia.DayOfWeek, tipo);
            for (var h = dia + inicio; h < dia + fim; h += Intervalo)
            {
                if (h < minimo) continue;
                lista.Add(new HorarioAppResponse
                {
                    Valor = h.ToString(Formato, CultureInfo.InvariantCulture),
                    Dia = RotuloDia(h, agora),
                    Hora = h.ToString("HH:mm", CultureInfo.InvariantCulture)
                });
            }
        }
        return lista;
    }

    /// <summary>
    /// Confere o horário enviado pelo app. Devolve a data e hora escolhidas ou lança
    /// RegraDeNegocioException com uma mensagem pronta para o cliente.
    /// </summary>
    public static DateTime Validar(string? horario, TipoEntrega tipo, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(horario) ||
            !DateTime.TryParseExact(horario.Trim(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var escolhido))
            throw new RegraDeNegocioException("Escolha o horário de " + Nome(tipo) + ".");

        // Na validação não se exige a antecedência: o cliente pode ter escolhido o horário
        // alguns minutos antes de enviar. Basta estar na janela do dia, no intervalo de 30 min,
        // ainda não ter passado e estar dentro do período de agendamento.
        var (inicio, fim) = Janela(escolhido.DayOfWeek, tipo);
        var hora = escolhido.TimeOfDay;
        var valido = hora >= inicio && hora < fim
                     && (hora - inicio).Ticks % Intervalo.Ticks == 0
                     && escolhido > agora
                     && escolhido.Date < agora.Date.AddDays(DiasParaAgendar);
        if (!valido)
            throw new RegraDeNegocioException(
                $"O horário {escolhido.ToString("dd/MM HH:mm", CultureInfo.InvariantCulture)} não está disponível para {Nome(tipo)}. Escolha outro horário.");

        return escolhido;
    }

    private static TimeSpan Hora(int h, int min = 0) => new(h, min, 0);

    private static string Nome(TipoEntrega tipo) => tipo == TipoEntrega.Entrega ? "entrega" : "retirada";

    private static string RotuloDia(DateTime h, DateTime agora)
    {
        if (h.Date == agora.Date) return "Hoje";
        if (h.Date == agora.Date.AddDays(1)) return "Amanhã";
        return $"{DiasSemana[(int)h.DayOfWeek]}, {h.ToString("dd/MM", CultureInfo.InvariantCulture)}";
    }
}

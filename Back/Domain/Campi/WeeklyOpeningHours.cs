namespace Estud.Back.Domain.Campi;

// TODO: colocar isso dentro do campus e testar com unit tests.
public class WeeklyOpeningHours(List<OpeningHour> hours)
{
    // Campus sem nenhuma linha fica fechado a semana inteira — é a leitura
    // literal de "ausência de linha significa fechado".
    private readonly Dictionary<Day, List<OpeningHour>> _byDay = hours
        .GroupBy(h => h.Day)
        .ToDictionary(g => g.Key, g => g.OrderBy(h => h.Start).ToList());

    private List<OpeningHour> WindowsOf(Day day)
        => _byDay.TryGetValue(day, out var windows) ? windows : [];

    /// <summary>
    /// Minutos abertos na interseção do dia com a janela do turno.
    /// </summary>
    public int MinutesOpenIn(Day day, Shift shift)
        => ClipToOpen(day, shift.StartInMinutes, shift.EndInMinutes);

    /// <summary>
    /// Recorta um intervalo ao que o campus está aberto naquele dia, devolvendo
    /// quantos minutos do intervalo caem dentro de alguma janela de funcionamento.
    /// As janelas de um dia nunca se sobrepõem, então somá-las não conta em dobro.
    /// </summary>
    public int ClipToOpen(Day day, int startInMinutes, int endInMinutes)
    {
        return WindowsOf(day).Sum(w => Overlap(
            w.Start.ToMinutes(), w.End.ToMinutes(), startInMinutes, endInMinutes));
    }

    // Minutos de interseção entre [aStart, aEnd) e [bStart, bEnd), ambos em
    // minutos a partir da meia-noite. Intervalos disjuntos dão 0.
    private static int Overlap(int aStart, int aEnd, int bStart, int bEnd)
        => Math.Max(Math.Min(aEnd, bEnd) - Math.Max(aStart, bStart), 0);
}

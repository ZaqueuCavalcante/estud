namespace Estud.Back.Domain.Campi;

/// <summary>
/// Uma janela de funcionamento de um campus num dia da semana. <br/>
/// Ausência de linha para um dia significa que o campus não abre naquele dia.
/// </summary>
public class OpeningHour
{
    public int Id { get; set; }
    public int CampusId { get; set; }
    public Day Day { get; set; }
    public Hour Start { get; set; }
    public Hour End { get; set; }

    public Campus? Campus { get; set; }

    private OpeningHour() { }

    public OpeningHour(Day day, Hour start, Hour end)
    {
        Day = day;
        Start = start;
        End = end;
    }

    public bool Overlaps(OpeningHour other)
    {
        if (Day != other.Day) return false;
        return Start < other.End && other.Start < End;
    }
}

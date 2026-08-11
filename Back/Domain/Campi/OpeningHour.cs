using Estud.Back.Domain.Classes;

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

    public static OneOf<OpeningHour, EstudError> New(Day day, Hour start, Hour end)
    {
        if (!day.IsValid()) return new InvalidDay();
        if (!start.IsValid()) return new InvalidHour();
        if (!end.IsValid()) return new InvalidHour();

        if (start == end || end < start) return new InvalidOpeningHour();

        return new OpeningHour(day, start, end);
    }

    public Schedule ToSchedule()
    {
        return Schedule.Window(Day, Start, End);
    }

    public bool Overlaps(OpeningHour other)
    {
        if (Day != other.Day) return false;
        return Start < other.End && other.Start < End;
    }
}

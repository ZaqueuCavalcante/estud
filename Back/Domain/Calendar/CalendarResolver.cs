namespace Estud.Back.Domain.Calendar;

/// <summary>
/// Resolve o tipo efetivo de cada dia aplicando os níveis do calendário.
/// </summary>
/// <remarks>
/// A precedência é campus → instituição → feriado nacional → fim de semana → dia letivo.
/// O primeiro nível que se manifesta sobre a data ganha, e os de baixo nem são consultados.
/// </remarks>
public class CalendarResolver
{
    private readonly Dictionary<DateOnly, CalendarDay> institutionDays;
    private readonly Dictionary<DateOnly, CalendarDay> campusDays;

    // Os feriados móveis dependem da Páscoa do ano, então o cálculo é por ano e
    // fica em cache: um range de aulas pode atravessar a virada do ano.
    private readonly Dictionary<int, Dictionary<DateOnly, string>> holidaysByYear = [];

    /// <summary>
    /// Monta o resolver para um escopo.
    /// </summary>
    /// <param name="days">
    /// Overrides do escopo: os dias da instituição mais os de, no máximo, um
    /// campus. Dias de campi diferentes no mesmo resolver é erro de chamada.
    /// </param>
    public CalendarResolver(IEnumerable<CalendarDay> days)
    {
        var byLevel = days.ToLookup(d => d.CampusId == null);

        institutionDays = byLevel[true].ToDictionary(d => d.Date);
        campusDays = byLevel[false].ToDictionary(d => d.Date);
    }

    public ResolvedCalendarDay Resolve(DateOnly date)
    {
        if (campusDays.TryGetValue(date, out var campusDay))
        {
            return new ResolvedCalendarDay(date, campusDay.DayType, campusDay.Description, CalendarDaySource.Campus);
        }

        if (institutionDays.TryGetValue(date, out var institutionDay))
        {
            return new ResolvedCalendarDay(date, institutionDay.DayType, institutionDay.Description, CalendarDaySource.Institution);
        }

        if (HolidaysOf(date.Year).TryGetValue(date, out var holiday))
        {
            return new ResolvedCalendarDay(date, DayType.Holiday, holiday, CalendarDaySource.Global);
        }

        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return new ResolvedCalendarDay(date, DayType.Weekend, null, CalendarDaySource.Weekend);
        }

        return new ResolvedCalendarDay(date, DayType.Default, null, CalendarDaySource.Default);
    }

    /// <summary>
    /// O id do override que existe no escopo consultado, ou nulo quando o dia é
    /// herdado de um nível acima.
    /// </summary>
    /// <remarks>
    /// É o que diz à UI se o dia pode ser editado/removido ali mesmo ou se só
    /// dá para sobrescrevê-lo.
    /// </remarks>
    public int? OverrideIdIn(DateOnly date, bool campusScope)
    {
        if (campusScope) return campusDays.TryGetValue(date, out var campusDay) ? campusDay.Id : null;

        return institutionDays.TryGetValue(date, out var institutionDay) ? institutionDay.Id : null;
    }

    public bool IsSchoolDay(DateOnly date) => Resolve(date).DayType == DayType.Default;

    private Dictionary<DateOnly, string> HolidaysOf(int year)
    {
        if (holidaysByYear.TryGetValue(year, out var cached)) return cached;

        var holidays = NationalHolidays.OfYear(year);
        holidaysByYear[year] = holidays;

        return holidays;
    }
}

public record ResolvedCalendarDay(
    DateOnly Date,
    DayType DayType,
    string? Description,
    CalendarDaySource Source
);

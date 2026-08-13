namespace Estud.Back.Features.Calendar.GetCalendar;

public class GetCalendarIn : IApiDto<GetCalendarIn>
{
    /// <summary>
    /// Ano do calendário. Quando não informado, usa o ano corrente.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Campus do calendário. Quando não informado, retorna o calendário da instituição.
    /// </summary>
    public int? CampusId { get; set; }

    public static IEnumerable<(string, GetCalendarIn)> GetExamples() =>
    [
        ("Instituição", new GetCalendarIn { Year = 2026 }),
        ("Campus", new GetCalendarIn { Year = 2026, CampusId = 1 }),
    ];
}

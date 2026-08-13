namespace Estud.Back.Features.Calendar.GetCalendar;

public class GetCalendarOut : IApiDto<GetCalendarOut>
{
    public int Year { get; set; }

    /// <summary>
    /// Campus do calendário. Nulo quando o calendário é o da instituição.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Nome do campus. Nulo quando o calendário é o da instituição.
    /// </summary>
    public string? Campus { get; set; }

    public int Total { get; set; }

    public List<GetCalendarItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetCalendarOut)> GetExamples() =>
    [
        ("Campus", new GetCalendarOut
        {
            Year = 2026,
            CampusId = 1,
            Campus = "Campus Central",
            Total = 3,
            Items =
            [
                new() { Date = new DateTime(2026, 1, 1), DayType = DayType.Holiday, Description = "Confraternização Universal", Source = CalendarDaySource.Global },
                new() { Id = 1, Date = new DateTime(2026, 1, 2), DayType = DayType.Vacation, Description = "Férias de verão", Source = CalendarDaySource.Institution },
                new() { Id = 7, Date = new DateTime(2026, 3, 19), DayType = DayType.Recess, Description = "Aniversário da cidade", Source = CalendarDaySource.Campus },
            ]
        }),
    ];
}

public class GetCalendarItemOut
{
    /// <summary>
    /// Id do override no nível consultado. Nulo quando o dia é herdado de um nível acima.
    /// </summary>
    public int? Id { get; set; }

    public DateTime Date { get; set; }

    public DayType DayType { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Nível de onde veio o tipo do dia.
    /// </summary>
    public CalendarDaySource Source { get; set; }
}

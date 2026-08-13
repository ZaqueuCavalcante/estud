namespace Estud.Back.Features.Calendar.CreateCalendarDay;

public class CreateCalendarDayOut : IApiDto<CreateCalendarDayOut>
{
    /// <summary>
    /// Ids dos dias customizados, na ordem das datas.
    /// </summary>
    public List<int> Ids { get; set; } = [];

    public int Total { get; set; }

    public static IEnumerable<(string, CreateCalendarDayOut)> GetExamples() =>
    [
        ("Um dia", new CreateCalendarDayOut { Ids = [1], Total = 1 }),
        ("Intervalo", new CreateCalendarDayOut { Ids = [1, 2, 3], Total = 3 }),
    ];
}

namespace Estud.Back.Features.Calendar.CreateCalendarDay;

public class CreateCalendarDayIn : IApiDto<CreateCalendarDayIn>
{
    /// <summary>
    /// Data do dia. Quando há data final, é o primeiro dia do intervalo.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Data final do intervalo, inclusive. Quando não informada, customiza só um dia.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Campus do override. Quando não informado, o override vale para a instituição inteira.
    /// </summary>
    public int? CampusId { get; set; }

    /// <summary>
    /// Tipo do dia
    /// </summary>
    public DayType? DayType { get; set; }

    /// <summary>
    /// Descrição do dia. Ex: "Semana de provas".
    /// </summary>
    public string? Description { get; set; }

    public static IEnumerable<(string, CreateCalendarDayIn)> GetExamples() =>
    [
        ("Férias da instituição",
        new CreateCalendarDayIn
        {
            Date = new DateTime(2026, 1, 5),
            EndDate = new DateTime(2026, 1, 31),
            DayType = Domain.Enums.DayType.Vacation,
            Description = "Férias de verão",
        }),

        ("Feriado de um campus",
        new CreateCalendarDayIn
        {
            Date = new DateTime(2026, 6, 24),
            CampusId = 1,
            DayType = Domain.Enums.DayType.Holiday,
            Description = "São João",
        }),

        ("Sábado letivo",
        new CreateCalendarDayIn
        {
            Date = new DateTime(2026, 5, 9),
            DayType = Domain.Enums.DayType.Default,
            Description = "Reposição de aulas",
        }),
    ];
}

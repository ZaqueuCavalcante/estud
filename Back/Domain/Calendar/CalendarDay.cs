namespace Estud.Back.Domain.Calendar;

/// <summary>
/// Override de um dia no calendário acadêmico.
/// </summary>
public class CalendarDay
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int? CampusId { get; set; }
    public DateOnly Date { get; set; }
    public DayType DayType { get; set; }
    public string? Description { get; set; }

    private CalendarDay() {}

    public CalendarDay(
        int institutionId,
        int? campusId,
        DateOnly date,
        DayType dayType,
        string? description = null
    ) {
        InstitutionId = institutionId;
        CampusId = campusId;
        Date = date;
        DayType = dayType;
        Description = description;
    }

    public void Update(DayType dayType, string? description)
    {
        DayType = dayType;
        Description = description;
    }
}

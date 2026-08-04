namespace Estud.Back.Features.Campi.GetCampusOpeningHours;

public class GetCampusOpeningHoursOut : IApiDto<GetCampusOpeningHoursOut>
{
    public int CampusId { get; set; }

    /// <summary>
    /// Nome do campus
    /// </summary>
    public string Campus { get; set; }

    /// <summary>
    /// Um item por dia da semana, sempre os 6, na ordem de segunda a sábado
    /// </summary>
    public List<CampusOpeningHoursDayOut> Days { get; set; } = [];

    public static IEnumerable<(string, GetCampusOpeningHoursOut)> GetExamples() =>
    [
        ("Exemplo", new GetCampusOpeningHoursOut
        {
            CampusId = 1,
            Campus = "Campus Agreste",
            Days =
            [
                new() { Day = Day.Monday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Tuesday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Wednesday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Thursday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Friday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Saturday, Windows = [] },
            ],
        }),
    ];
}

public class CampusOpeningHoursDayOut
{
    public Day Day { get; set; }

    /// <summary>
    /// Janelas de funcionamento do dia, ordenadas pelo início.
    /// Vazia = o campus não abre neste dia.
    /// </summary>
    public List<CampusOpeningHoursWindowOut> Windows { get; set; } = [];
}

public class CampusOpeningHoursWindowOut
{
    public Hour Start { get; set; }
    public Hour End { get; set; }
}

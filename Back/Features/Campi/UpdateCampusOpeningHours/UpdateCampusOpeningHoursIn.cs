namespace Estud.Back.Features.Campi.UpdateCampusOpeningHours;

public class UpdateCampusOpeningHoursIn : IApiDto<UpdateCampusOpeningHoursIn>
{
    /// <summary>
    /// A semana inteira. Substitui a configuração atual (replace-all).
    /// Dia omitido, ou com Windows vazia, significa que o campus não abre nele.
    /// </summary>
    public List<UpdateCampusOpeningHoursDayIn> Days { get; set; } = [];

    public static IEnumerable<(string, UpdateCampusOpeningHoursIn)> GetExamples() =>
    [
        ("Exemplo", new UpdateCampusOpeningHoursIn
        {
            Days =
            [
                new() { Day = Day.Monday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Tuesday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Wednesday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Thursday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Friday, Windows = [new() { Start = Hour.H07_00, End = Hour.H22_00 }] },
                new() { Day = Day.Saturday, Windows = [new() { Start = Hour.H08_00, End = Hour.H12_00 }] },
            ],
        }),
    ];
}

public class UpdateCampusOpeningHoursDayIn
{
    public Day Day { get; set; }
    public List<UpdateCampusOpeningHoursWindowIn> Windows { get; set; } = [];
}

public class UpdateCampusOpeningHoursWindowIn
{
    public Hour Start { get; set; }
    public Hour End { get; set; }
}

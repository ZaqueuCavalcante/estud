namespace Estud.Back.Features.Campi.CreateCampus;

public class CreateCampusIn : IApiDto<CreateCampusIn>
{
    public string Name { get; set; }
    public BrazilState? State { get; set; }
    public string City { get; set; }

    public static IEnumerable<(string, CreateCampusIn)> GetExamples() =>
    [
        ("Agreste",
        new CreateCampusIn
        {
            Name = "Agreste",
            State = BrazilState.PE,
            City = "Caruaru",
        }),

        ("Suassuna",
        new CreateCampusIn
        {
            Name = "Suassuna",
            State = BrazilState.PE,
            City = "Recife",
        }),
    ];
}

namespace Estud.Back.Features.Campi.GetCampi;

public class GetCampiOut : IApiDto<GetCampiOut>
{
    public int Total { get; set; }
    public List<GetCampiItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetCampiOut)> GetExamples() =>
    [
        ("Campi",
        new GetCampiOut()
        {
            Total = 2,
            Items =
            [
                new GetCampiItemOut
                {
                    Id = 1,
                    Name = "Agreste",
                    State = BrazilState.PE,
                    City = "Caruaru",
                    UsedMinutesRate = 0.75m,
                    UsedCapacityRate = 0.5m,
                },
                new GetCampiItemOut
                {
                    Id = 2,
                    Name = "Suassuna",
                    State = BrazilState.PE,
                    City = "Recife",
                    UsedMinutesRate = 0.25m,
                    UsedCapacityRate = 0.75m,
                },
            ],
        }),
    ];
}

public class GetCampiItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public BrazilState State { get; set; }
    public string City { get; set; }
    public decimal UsedMinutesRate { get; set; }
    public decimal UsedCapacityRate { get; set; }
}

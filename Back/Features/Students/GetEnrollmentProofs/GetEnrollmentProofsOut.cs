namespace Estud.Back.Features.Students.GetEnrollmentProofs;

public class GetEnrollmentProofsOut : IApiDto<GetEnrollmentProofsOut>
{
    public int Total { get; set; }
    public List<GetEnrollmentProofsItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetEnrollmentProofsOut)> GetExamples() =>
    [
        ("Exemplo", new GetEnrollmentProofsOut
        {
            Total = 1,
            Items =
            [
                new()
                {
                    Code = "ESTUD-2026-3F9A1B7C2D",
                    IssuedAt = new DateTime(2026, 2, 10, 13, 30, 0, DateTimeKind.Utc),
                },
            ],
        }),
    ];
}

public class GetEnrollmentProofsItemOut
{
    /// <summary>
    /// Código de verificação do comprovante.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Data e hora de emissão do comprovante (UTC).
    /// </summary>
    public DateTime IssuedAt { get; set; }
}

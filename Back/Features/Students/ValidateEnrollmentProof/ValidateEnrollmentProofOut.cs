namespace Estud.Back.Features.Students.ValidateEnrollmentProof;

public class ValidateEnrollmentProofOut : IApiDto<ValidateEnrollmentProofOut>
{
    /// <summary>
    /// Código de verificação do comprovante.
    /// </summary>
    public string Code { get; set; }

    public string Institution { get; set; }
    public string StudentName { get; set; }

    /// <summary>
    /// Matrícula do aluno.
    /// </summary>
    public string EnrollmentCode { get; set; }

    public string Course { get; set; }
    public string Campus { get; set; }
    public string Period { get; set; }
    public CourseSession Session { get; set; }

    /// <summary>
    /// Data e hora de emissão do comprovante (UTC).
    /// </summary>
    public DateTime IssuedAt { get; set; }

    public static IEnumerable<(string, ValidateEnrollmentProofOut)> GetExamples() =>
    [
        ("Exemplo", new ValidateEnrollmentProofOut
        {
            Code = "ESTUD-2026-3F9A1B7C2D",
            Institution = "UFAL",
            StudentName = "Maria Souza",
            EnrollmentCode = "20251A2B3C4D",
            Course = "Análise e Desenvolvimento de Sistemas",
            Campus = "Campus Maceió",
            Period = "2026.1",
            Session = CourseSession.Evening,
            IssuedAt = new DateTime(2026, 2, 10, 13, 30, 0, DateTimeKind.Utc),
        }),
    ];
}

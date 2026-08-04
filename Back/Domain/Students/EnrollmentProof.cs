namespace Estud.Back.Domain.Students;

/// <summary>
/// Registro de emissão de um Comprovante de Matrícula.
/// <br/> <br/>
/// O PDF em si não é persistido — o que fica salvo é este registro, com o snapshot dos dados
/// no momento da emissão e um <see cref="Code"/> único de verificação. A autenticidade do
/// comprovante é conferida publicamente informando esse código.
/// </summary>
public class EnrollmentProof
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int StudentId { get; set; }

    /// <summary>
    /// Código de verificação único do comprovante (ex: ESTUD-2026-3F9A1B7C2D).
    /// </summary>
    public string Code { get; set; }

    public DateTime IssuedAt { get; set; }

    public EnrollmentProofMetadata Metadata { get; set; }

    private EnrollmentProof() {}

    public EnrollmentProof(
        int institutionId,
        int studentId,
        EnrollmentProofMetadata metadata
    ) {
        InstitutionId = institutionId;
        StudentId = studentId;
        Metadata = metadata;
        IssuedAt = DateTime.UtcNow;
        Code = $"ESTUD-{IssuedAt.Year}-{Guid.NewGuid().ToString("N")[..16].ToUpper()}";
    }
}

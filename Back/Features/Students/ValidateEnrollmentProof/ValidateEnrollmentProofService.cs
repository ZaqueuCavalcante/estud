namespace Estud.Back.Features.Students.ValidateEnrollmentProof;

public class ValidateEnrollmentProofService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<ValidateEnrollmentProofOut, EstudError>> Validate(string code)
    {
        if (code.IsEmpty()) return EnrollmentProofNotFound.I;

        // Endpoint público: a busca é apenas pelo código (globalmente único), sem escopo de instituição.
        // Metadata é uma coluna jsonb com value converter — não dá pra projetar seus campos dentro da
        // query (o EF não traduz), então materializamos a entidade e montamos o DTO em memória.
        var proof = await ctx.EnrollmentProofs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code);

        if (proof == null) return EnrollmentProofNotFound.I;

        return new ValidateEnrollmentProofOut
        {
            Code = proof.Code,
            Institution = proof.Metadata.Institution,
            StudentName = proof.Metadata.StudentName,
            EnrollmentCode = proof.Metadata.StudentEnrollmentCode,
            Course = proof.Metadata.Course,
            Campus = proof.Metadata.Campus,
            Period = proof.Metadata.Period,
            Session = proof.Metadata.Session,
            IssuedAt = proof.IssuedAt,
        };
    }
}

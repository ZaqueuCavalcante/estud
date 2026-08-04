using QuestPDF.Fluent;
using Estud.Back.Domain.Students;

namespace Estud.Back.Features.Students.GenerateEnrollmentProof;

public class GenerateEnrollmentProofService(EstudDbContext ctx, FrontendSettings frontend) : IEstudService
{
    public async Task<OneOf<EnrollmentProofFile, EstudError>> Generate()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;

        var studentId = await ctx.GetStudentId(institutionId, userId);
        if (studentId == 0) return StudentNotFound.I;

        var student = await ctx.Students.AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.Name, s.EnrollmentCode })
            .FirstAsync();

        // Oferta de curso atual do aluno (vínculo ativo mais recente)
        var enrollment = await ctx.StudentCourseEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.LeftAt == null)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new
            {
                Course = e.CourseOffering!.Course!.Name,
                Campus = e.CourseOffering.Campus!.Name,
                Period = e.CourseOffering.AcademicPeriod!.Name,
                e.CourseOffering.Session,
            })
            .FirstOrDefaultAsync();
        if (enrollment == null) return StudentNotEnrolledInAnyCourse.I;

        var institution = await ctx.Institutions.AsNoTracking()
            .Where(i => i.Id == institutionId)
            .Select(i => i.Name)
            .FirstAsync();

        var metadata = new EnrollmentProofMetadata(
            student.Name,
            student.EnrollmentCode,
            institution,
            enrollment.Course,
            enrollment.Campus,
            enrollment.Period,
            enrollment.Session
        );
        var proof = new EnrollmentProof(institutionId, studentId, metadata);
        await ctx.SaveChangesAsync(proof);

        var validationUrl = frontend.BuildUrl($"/validar-comprovante?codigo={proof.Code}");

        var pdf = new EnrollmentProofDocument(proof, validationUrl).GeneratePdf();

        return new EnrollmentProofFile(pdf, $"comprovante-matricula-{proof.Code}.pdf");
    }
}

public record EnrollmentProofFile(byte[] Content, string FileName);

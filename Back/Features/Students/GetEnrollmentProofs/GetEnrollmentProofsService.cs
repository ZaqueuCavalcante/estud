namespace Estud.Back.Features.Students.GetEnrollmentProofs;

public class GetEnrollmentProofsService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetEnrollmentProofsOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var proofs = await ctx.EnrollmentProofs.AsNoTracking()
            .Where(p => p.InstitutionId == institutionId && p.StudentId == studentId)
            .OrderByDescending(p => p.IssuedAt)
            .Select(p => new GetEnrollmentProofsItemOut
            {
                Code = p.Code,
                IssuedAt = p.IssuedAt,
            })
            .ToListAsync();

        return new GetEnrollmentProofsOut
        {
            Total = proofs.Count,
            Items = proofs,
        };
    }
}

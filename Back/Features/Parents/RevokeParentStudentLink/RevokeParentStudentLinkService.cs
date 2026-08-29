namespace Estud.Back.Features.Parents.RevokeParentStudentLink;

public class RevokeParentStudentLinkService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Revoke(int parentId, int studentId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var link = await ctx.ParentStudents
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.ParentId == parentId && x.StudentId == studentId);
        if (link == null) return ParentStudentLinkNotFound.I;

        if (link.Status == ParentStudentStatus.Revoked) return ParentStudentLinkAlreadyRevoked.I;

        link.Revoke();
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

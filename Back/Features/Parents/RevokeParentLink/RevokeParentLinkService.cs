namespace Estud.Back.Features.Parents.RevokeParentLink;

public class RevokeParentLinkService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Revoke(int parentId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;

        var studentId = await ctx.GetStudentId(institutionId, userId);

        var link = await ctx.ParentStudents
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.ParentId == parentId && x.StudentId == studentId);
        if (link == null) return ParentStudentLinkNotFound.I;

        var birthdate = await ctx.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => x.Birthdate).FirstAsync();
        if (birthdate == null || !birthdate.Value.IsAdult()) return StudentMustBeAdult.I;

        if (link.RevokedByStudent) return ParentStudentLinkAlreadyRevoked.I;

        link.RevokeByStudent();
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

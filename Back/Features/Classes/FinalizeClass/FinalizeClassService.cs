namespace Estud.Back.Features.Classes.FinalizeClass;

public class FinalizeClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Finalize(int classId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        if (@class.Status == ClassStatus.Finalized) return ClassAlreadyFinalized.I;
        if (@class.Status != ClassStatus.Started) return ClassMustBeStarted.I;

        @class.Status = ClassStatus.Finalized;
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

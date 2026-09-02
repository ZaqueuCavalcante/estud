namespace Estud.Back.Features.Teachers.AddActivityNote;

public class AddActivityNoteService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Add(int activityId, int workId, AddActivityNoteIn data)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var activity = await ctx.ClassActivities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == activityId);
        if (activity == null) return ClassActivityNotFound.I;

        var classOk = await ctx.Classes.AnyAsync(c => c.Id == activity.ClassId && c.InstitutionId == institutionId);
        if (!classOk) return ClassActivityNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == activity.ClassId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var work = await ctx.ClassActivityWorks.FirstOrDefaultAsync(w => w.Id == workId && w.ClassActivityId == activityId);
        if (work == null) return ClassActivityWorkNotFound.I;

        var result = work.AddNote(data.Note);
        if (result.IsError) return result.Error;

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

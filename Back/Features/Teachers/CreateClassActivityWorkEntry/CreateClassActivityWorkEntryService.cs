namespace Estud.Back.Features.Teachers.CreateClassActivityWorkEntry;

public class CreateClassActivityWorkEntryService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<CreateClassActivityWorkEntryIn>
    {
        public Validator()
        {
            RuleFor(x => x.Content).MaximumLength(10000).WithError(InvalidClassActivityWorkContent.I);
            RuleFor(x => x.Status).IsInEnum().WithError(InvalidClassActivityWorkEntry.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Create(int activityId, int workId, CreateClassActivityWorkEntryIn data)
    {
        if (V.Run(data, out var error)) return error;

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

        var result = work.AddTeacherEntry(userId, data.Content, data.Note, data.Status);
        if (result.IsError) return result.Error;

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

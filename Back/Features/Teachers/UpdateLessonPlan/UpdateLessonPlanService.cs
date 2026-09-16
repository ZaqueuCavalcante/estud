namespace Estud.Back.Features.Teachers.UpdateLessonPlan;

public class UpdateLessonPlanService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<UpdateLessonPlanIn>
    {
        public Validator()
        {
            RuleFor(x => x.PlannedContent).MaximumLength(2000).WithError(InvalidClassLessonPlan.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Update(int lessonId, UpdateLessonPlanIn data)
    {
        if (V.Run(data, out var error)) return error;

        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var lesson = await ctx.ClassLessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.Class.InstitutionId == institutionId);
        if (lesson == null) return ClassLessonNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == lesson.ClassId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        lesson.UpdatePlan(data.PlannedContent);

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

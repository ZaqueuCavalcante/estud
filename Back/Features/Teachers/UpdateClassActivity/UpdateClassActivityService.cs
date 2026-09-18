using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Teachers.UpdateClassActivity;

public class UpdateClassActivityService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Update(int classId, int activityId, UpdateClassActivityIn data)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var exists = await ctx.Classes.AnyAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (!exists) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var activity = await ctx.ClassActivities.FirstOrDefaultAsync(a => a.Id == activityId && a.ClassId == classId);
        if (activity == null) return ClassActivityNotFound.I;

        var gradeRule = await ctx.InstitutionConfigs.AsNoTracking()
            .Where(c => c.InstitutionId == institutionId)
            .Select(c => c.GradeRule)
            .FirstAsync();
        if (!gradeRule.NoteTypes.Contains(data.Note)) return NoteTypeNotUsedByInstitution.I;

        var result = activity.Update(
            data.Note,
            data.Title,
            data.Description,
            data.Type,
            data.Weight,
            data.DueDate,
            data.DueHour
        );
        if (result.IsError) return result.Error;

        var noteWeight = await ctx.ClassActivities.AsNoTracking()
            .Where(a => a.ClassId == classId && a.Note == data.Note && a.Id != activityId)
            .SumAsync(a => a.Weight);
        if (noteWeight + data.Weight > 100) return InvalidClassActivityWeight.I;

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

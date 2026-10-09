namespace Estud.Back.Features.Classes.GetClassSchedules;

public class GetClassSchedulesService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetClassSchedulesOut, EstudError>> Get(int classId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var @class = await ctx.Classes.AsNoTracking()
            .Include(c => c.Teachers)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var teacherIds = @class.Teachers.Select(t => t.Id).ToList();
        var teacherNames = @class.Teachers.ToDictionary(t => t.Id, t => t.Name);

        var schedules = await ctx.Schedules.AsNoTracking()
            .Where(s => s.ClassId == classId
                || (s.TeacherId != null && teacherIds.Contains(s.TeacherId.Value)
                    && s.Class!.InstitutionId == institutionId && s.Class.Status != ClassStatus.Finalized))
            .Select(s => new
            {
                ClassId = s.ClassId!.Value,
                Discipline = s.Class!.Discipline.Name,
                s.Day,
                s.Start,
                s.End,
                s.TeacherId,
                s.ClassroomId,
                Classroom = s.Classroom!.Name,
            })
            .ToListAsync();

        return new GetClassSchedulesOut
        {
            Schedules = schedules
                .OrderBy(s => s.Day).ThenBy(s => s.Start).ThenBy(s => s.ClassId != classId)
                .Select(s => new GetClassSchedulesItemOut
                {
                    ClassId = s.ClassId,
                    Discipline = s.Discipline,
                    FromOtherClass = s.ClassId != classId,
                    Day = s.Day,
                    StartAt = s.Start,
                    EndAt = s.End,
                    TeacherId = s.TeacherId,
                    Teacher = s.TeacherId != null && teacherNames.TryGetValue(s.TeacherId.Value, out var name) ? name : null,
                    ClassroomId = s.ClassroomId,
                    Classroom = s.Classroom,
                })
                .ToList(),
        };
    }
}

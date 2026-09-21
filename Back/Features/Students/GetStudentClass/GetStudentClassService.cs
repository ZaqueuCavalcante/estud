namespace Estud.Back.Features.Students.GetStudentClass;

public class GetStudentClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentClassOut, EstudError>> Get(int classId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .Include(c => c.Discipline)
            .Include(c => c.Teachers)
            .Include(c => c.Period)
            .Include(c => c.Schedules)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var classStudent = await ctx.ClassStudents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassId == classId && x.StudentId == studentId);
        if (classStudent == null) return StudentNotEnrolledInClass.I;

        var classroomIds = @class.Schedules
            .Where(s => s.ClassroomId != null)
            .Select(s => s.ClassroomId!.Value)
            .Distinct()
            .ToList();
        var classroomNames = await ctx.Classrooms.AsNoTracking()
            .Where(c => classroomIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return new GetStudentClassOut
        {
            Id = @class.Id,
            Discipline = @class.Discipline?.Name ?? "",
            Period = @class.Period?.Name ?? "",
            Workload = @class.Workload,
            Status = @class.Status,
            MyStatus = classStudent.Status,
            Teachers = @class.Teachers.Select(t => t.Name).Order().ToList(),
            Schedules = @class.Schedules
                .OrderBy(s => s.Day).ThenBy(s => s.Start)
                .Select(s => new GetStudentClassScheduleOut(s.Day, s.Start, s.End)
                {
                    TeacherId = s.TeacherId,
                    Teacher = s.TeacherId == null ? null : @class.Teachers.FirstOrDefault(t => t.Id == s.TeacherId)?.Name,
                    ClassroomId = s.ClassroomId,
                    Classroom = s.ClassroomId != null && classroomNames.TryGetValue(s.ClassroomId.Value, out var name) ? name : null,
                })
                .ToList(),
        };
    }
}

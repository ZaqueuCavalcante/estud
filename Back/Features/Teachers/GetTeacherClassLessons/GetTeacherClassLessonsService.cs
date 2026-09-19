namespace Estud.Back.Features.Teachers.GetTeacherClassLessons;

public class GetTeacherClassLessonsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetTeacherClassLessonsOut, EstudError>> Get(int classId, GetTeacherClassLessonsIn query)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var lessonsQuery = ctx.ClassLessons.AsNoTracking()
            .Where(l => l.ClassId == classId);

        var search = query.Search?.Trim();
        if (search.HasValue()) lessonsQuery = lessonsQuery.Where(l => l.PlannedContent != null && EF.Functions.ILike(l.PlannedContent, $"%{search}%"));

        var lessons = await lessonsQuery
            .OrderBy(l => l.Number)
            .Select(l => new GetTeacherClassLessonsItemOut
            {
                Id = l.Id,
                Number = l.Number,
                Date = l.Date,
                StartAt = l.StartAt,
                EndAt = l.EndAt,
                Status = l.Status,
                PlannedContent = l.PlannedContent,
                PresentStudents = l.Attendances.Where(a => a.Present).Select(a => a.StudentId).ToList(),
            })
            .ToListAsync();

        return new GetTeacherClassLessonsOut { Lessons = lessons };
    }
}

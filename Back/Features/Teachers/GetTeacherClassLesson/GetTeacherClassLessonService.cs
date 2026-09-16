
namespace Estud.Back.Features.Teachers.GetTeacherClassLesson;

public class GetTeacherClassLessonService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetTeacherClassLessonOut, EstudError>> Get(int classId, int lessonId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var teacherId = await ctx.GetTeacherId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking()
            .Include(c => c.Discipline)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var assigned = await ctx.ClassTeachers.AnyAsync(ct => ct.ClassId == classId && ct.TeacherId == teacherId);
        if (!assigned) return TeacherNotAssignedToClass.I;

        var lesson = await ctx.ClassLessons.AsNoTracking()
            .Include(l => l.Attendances)
            .FirstOrDefaultAsync(l => l.Id == lessonId && l.ClassId == classId);
        if (lesson == null) return ClassLessonNotFound.I;

        var students = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.ClassId == classId && cs.Status == StudentClassStatus.Matriculado)
            .OrderBy(cs => cs.Student!.Name)
            .Select(cs => new GetTeacherClassLessonStudentOut
            {
                Id = cs.StudentId,
                Name = cs.Student!.Name,
            })
            .ToListAsync();

        var presences = lesson.Attendances.Where(a => a.Present).Select(a => a.StudentId).ToHashSet();
        foreach (var student in students)
        {
            student.Present = presences.Contains(student.Id);
        }

        return new GetTeacherClassLessonOut
        {
            Id = lesson.Id,
            ClassId = lesson.ClassId,
            Discipline = @class.Discipline?.Name ?? "",
            Number = lesson.Number,
            Date = lesson.Date,
            StartAt = lesson.StartAt,
            EndAt = lesson.EndAt,
            Status = lesson.Status,
            PlannedContent = lesson.PlannedContent,
            Students = students,
        };
    }
}

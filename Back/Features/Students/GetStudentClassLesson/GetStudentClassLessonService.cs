namespace Estud.Back.Features.Students.GetStudentClassLesson;

public class GetStudentClassLessonService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentClassLessonOut, EstudError>> Get(int classId, int lessonId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking().Include(c => c.Discipline)
            .FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var lesson = await ctx.ClassLessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lessonId && l.ClassId == classId);
        if (lesson == null) return ClassLessonNotFound.I;

        return new GetStudentClassLessonOut
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
        };
    }
}

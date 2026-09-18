namespace Estud.Back.Features.Students.GetStudentClassLessons;

public class GetStudentClassLessonsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentClassLessonsOut, EstudError>> Get(int classId)
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var @class = await ctx.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        var enrolled = await ctx.ClassStudents.AsNoTracking().AnyAsync(cs => cs.ClassId == classId && cs.StudentId == studentId);
        if (!enrolled) return StudentNotEnrolledInClass.I;

        var lessons = await ctx.ClassLessons.AsNoTracking()
            .Where(l => l.ClassId == classId)
            .OrderBy(l => l.Number)
            .Select(l => new GetStudentClassLessonsItemOut
            {
                Id = l.Id,
                Number = l.Number,
                Date = l.Date,
                StartAt = l.StartAt,
                EndAt = l.EndAt,
                Status = l.Status,
            })
            .ToListAsync();

        return new GetStudentClassLessonsOut { Lessons = lessons };
    }
}

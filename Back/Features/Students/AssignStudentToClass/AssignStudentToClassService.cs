using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Students.AssignStudentToClass;

public class AssignStudentToClassService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Assign(int studentId, AssignStudentToClassIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var studentOk = await ctx.Students.AnyAsync(s => s.Id == studentId && s.InstitutionId == institutionId);
        if (!studentOk) return StudentNotFound.I;

        var @class = await ctx.Classes.FirstOrDefaultAsync(c => c.Id == data.ClassId && c.InstitutionId == institutionId);
        if (@class == null) return ClassNotFound.I;

        if (@class.Status == ClassStatus.Finalized) return ClassAlreadyFinalized.I;

        var alreadyEnrolled = await ctx.ClassStudents.AnyAsync(x => x.ClassId == @class.Id && x.StudentId == studentId);
        if (alreadyEnrolled) return StudentAlreadyEnrolledInClass.I;

        var enrolledStudents = await ctx.ClassStudents.CountAsync(x => x.ClassId == @class.Id);
        if (enrolledStudents >= @class.Vacancies) return NoVacanciesInClass.I;

        ctx.Add(new ClassStudent(@class.Id, studentId));

        var activities = await ctx.ClassActivities.Where(a => a.ClassId == @class.Id).Select(a => a.Id).ToListAsync();
        foreach (var activityId in activities)
            ctx.Add(new ClassActivityWork(activityId, studentId));

        var lessons = await ctx.ClassLessons.Where(l => l.ClassId == @class.Id && l.Status == ClassLessonStatus.Finalized).Select(l => l.Id).ToListAsync();
        foreach (var lessonId in lessons)
            ctx.Add(new ClassLessonAttendance(@class.Id, lessonId, studentId, present: false));

        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

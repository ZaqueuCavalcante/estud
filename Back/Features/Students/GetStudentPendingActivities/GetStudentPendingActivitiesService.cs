namespace Estud.Back.Features.Students.GetStudentPendingActivities;

public class GetStudentPendingActivitiesService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetStudentPendingActivitiesOut> Get()
    {
        var userId = ctx.RequestUser.Id;
        var institutionId = ctx.RequestUser.InstitutionId;
        var studentId = await ctx.GetStudentId(institutionId, userId);

        var total = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.StudentId == studentId && cs.Status == StudentClassStatus.Matriculado
                && cs.Class!.InstitutionId == institutionId && cs.Class.Status == ClassStatus.Started)
            .SelectMany(cs => cs.Class!.Activities)
            .Where(a => a.ActivityType != ClassActivityType.Exam)
            .Where(a => a.Works.Any(w => w.StudentId == studentId && w.Status == ClassActivityWorkStatus.Pending))
            .CountAsync();

        return new GetStudentPendingActivitiesOut { Total = total };
    }
}

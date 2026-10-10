using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Insights.GetPendingAttendance;

public class GetPendingAttendanceService(EstudDbContext ctx) : IEstudService
{
    private const int Limit = 5;

    public async Task<OneOf<GetPendingAttendanceOut, EstudError>> Get(GetPendingAttendanceIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.Id == data.PeriodId && p.InstitutionId == institutionId);
        if (!periodExists) return AcademicPeriodNotFound.I;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastLessons = ctx.ClassLessons.AsNoTracking()
            .Where(l => l.Class.InstitutionId == institutionId && l.Class.PeriodId == data.PeriodId && l.Date < today);
        var pendingLessons = pastLessons.Where(l => l.Status == ClassLessonStatus.Pending);

        var pastCount = await pastLessons.CountAsync();
        var pendingCount = await pendingLessons.CountAsync();

        var pendingByClass = await pendingLessons.GroupBy(l => l.ClassId)
            .Select(g => new { ClassId = g.Key, PendingLessons = g.Count(), OldestPendingAt = g.Min(l => l.Date) })
            .OrderByDescending(x => x.PendingLessons).ThenBy(x => x.OldestPendingAt).ThenBy(x => x.ClassId)
            .Take(Limit)
            .ToListAsync();

        var classIds = pendingByClass.Select(x => x.ClassId).ToList();
        var disciplines = await ctx.Classes.AsNoTracking()
            .Where(c => classIds.Contains(c.Id))
            .Select(c => new { c.Id, Discipline = c.Discipline.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Discipline);

        return new GetPendingAttendanceOut
        {
            PastLessons = pastCount,
            PendingLessons = pendingCount,
            UpToDate = pastCount > 0 ? AttendanceRate.Of(pastCount - pendingCount, pastCount) : 100,
            Classes = pendingByClass
                .Select(x => new GetPendingAttendanceClassOut
                {
                    Id = x.ClassId,
                    Discipline = disciplines[x.ClassId],
                    PendingLessons = x.PendingLessons,
                })
                .ToList(),
        };
    }
}

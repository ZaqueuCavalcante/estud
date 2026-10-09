using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Insights.GetAttendance;

public class GetAttendanceService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetAttendanceOut, EstudError>> Get(GetAttendanceIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.Id == data.PeriodId && p.InstitutionId == institutionId);
        if (!periodExists) return AcademicPeriodNotFound.I;

        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var days = await GetDays(institutionId, data.PeriodId);
        var classes = await GetClasses(institutionId, data.PeriodId);
        var belowLimitClasses = classes.Count(c => AttendanceRate.Of(c.Presences, c.Presences + c.Absences) < config.FrequencyLimit);

        var presences = days.Sum(d => d.Presences);
        var total = presences + days.Sum(d => d.Absences);

        return new GetAttendanceOut
        {
            Average = AttendanceRate.Of(presences, total),
            BelowLimitClasses = belowLimitClasses,
            AboveLimitClasses = classes.Count - belowLimitClasses,
            Days = days
                .Select(d => new GetAttendanceDayOut
                {
                    Date = d.Date,
                    Attendance = AttendanceRate.Of(d.Presences, d.Presences + d.Absences),
                })
                .ToList(),
        };
    }

    private async Task<List<GetAttendanceDayDto>> GetDays(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                cl.date                                      AS date,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.class_lesson_attendances cla
            INNER JOIN
                estud.class_lessons cl ON cl.id = cla.lesson_id
            INNER JOIN
                estud.classes c ON c.id = cla.class_id
            WHERE
                c.institution_id = {0}
                    AND
                c.period_id = {1}
            GROUP BY
                cl.date
            ORDER BY
                cl.date
        ";

        return await ctx.Database
            .SqlQueryRaw<GetAttendanceDayDto>(sql, institutionId, periodId)
            .AsNoTracking().ToListAsync();
    }

    private async Task<List<GetAttendanceClassDto>> GetClasses(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                c.id                                         AS id,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.class_lesson_attendances cla
            INNER JOIN
                estud.classes c ON c.id = cla.class_id
            WHERE
                c.institution_id = {0}
                    AND
                c.period_id = {1}
            GROUP BY
                c.id
        ";

        return await ctx.Database
            .SqlQueryRaw<GetAttendanceClassDto>(sql, institutionId, periodId)
            .AsNoTracking().ToListAsync();
    }
}

using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Insights.GetLowestAttendanceClasses;

public class GetLowestAttendanceClassesService(EstudDbContext ctx) : IEstudService
{
    private const int Limit = 5;

    public async Task<OneOf<GetLowestAttendanceClassesOut, EstudError>> Get(GetLowestAttendanceClassesIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.Id == data.PeriodId && p.InstitutionId == institutionId);
        if (!periodExists) return AcademicPeriodNotFound.I;

        var classes = await GetClasses(institutionId, data.PeriodId);

        return new GetLowestAttendanceClassesOut
        {
            Classes = classes
                .Select(c => new GetLowestAttendanceClassesItemOut
                {
                    Id = c.Id,
                    Discipline = c.Discipline,
                    Attendance = AttendanceRate.Of(c.Presences, c.Presences + c.Absences),
                })
                .ToList(),
        };
    }

    private async Task<List<GetLowestAttendanceClassDto>> GetClasses(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                c.id                                         AS id,
                d.name                                       AS discipline,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.classes c
            INNER JOIN
                estud.disciplines d ON d.id = c.discipline_id
            INNER JOIN
                estud.class_lesson_attendances cla ON cla.class_id = c.id
            WHERE
                c.institution_id = {0}
                    AND
                c.period_id = {1}
            GROUP BY
                c.id, d.name
            ORDER BY
                count(cla.id) FILTER (WHERE cla.present)::numeric / count(cla.id), d.name, c.id
            LIMIT {2}
        ";

        return await ctx.Database
            .SqlQueryRaw<GetLowestAttendanceClassDto>(sql, institutionId, periodId, Limit)
            .AsNoTracking().ToListAsync();
    }
}

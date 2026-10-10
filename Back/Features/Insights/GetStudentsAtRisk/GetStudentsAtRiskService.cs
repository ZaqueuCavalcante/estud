using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Insights.GetStudentsAtRisk;

public class GetStudentsAtRiskService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetStudentsAtRiskOut, EstudError>> Get(GetStudentsAtRiskIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.Id == data.PeriodId && p.InstitutionId == institutionId);
        if (!periodExists) return AcademicPeriodNotFound.I;

        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var totalStudents = await ctx.ClassStudents.AsNoTracking()
            .Where(cs => cs.Class!.InstitutionId == institutionId && cs.Class.PeriodId == data.PeriodId
                && cs.Status == StudentClassStatus.Matriculado)
            .Select(cs => cs.StudentId)
            .Distinct()
            .CountAsync();

        var attendances = await GetAttendances(institutionId, data.PeriodId);
        var byFrequency = attendances
            .Where(a => AttendanceRate.Of(a.Presences, a.Presences + a.Absences) < config.FrequencyLimit)
            .Select(a => a.StudentId)
            .ToHashSet();

        var works = await GetWorks(institutionId, data.PeriodId);
        var byGrade = works
            .GroupBy(w => (w.ClassId, w.StudentId))
            .Where(g => Math.Round(config.GradeRule.Average(g.Select(w => (w.NoteType, w.Weight, w.Note))), 1, MidpointRounding.AwayFromZero) < config.NoteLimit)
            .Select(g => g.Key.StudentId)
            .ToHashSet();

        var both = byFrequency.Intersect(byGrade).Count();

        return new GetStudentsAtRiskOut
        {
            TotalStudents = totalStudents,
            OnlyFrequency = byFrequency.Count - both,
            OnlyGrade = byGrade.Count - both,
            Both = both,
        };
    }

    private async Task<List<GetStudentsAtRiskAttendanceDto>> GetAttendances(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                cs.student_id                                AS student_id,
                count(cla.id) FILTER (WHERE cla.present)     AS presences,
                count(cla.id) FILTER (WHERE NOT cla.present) AS absences
            FROM
                estud.classes__students cs
            INNER JOIN
                estud.classes c ON c.id = cs.class_id
            INNER JOIN
                estud.class_lesson_attendances cla ON cla.class_id = cs.class_id AND cla.student_id = cs.student_id
            WHERE
                c.institution_id = {0}
                    AND
                c.period_id = {1}
                    AND
                cs.status = {2}
            GROUP BY
                cs.class_id, cs.student_id
        ";

        return await ctx.Database
            .SqlQueryRaw<GetStudentsAtRiskAttendanceDto>(sql, institutionId, periodId, (int)StudentClassStatus.Matriculado)
            .AsNoTracking().ToListAsync();
    }

    private async Task<List<GetStudentsAtRiskWorkDto>> GetWorks(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                cs.class_id           AS class_id,
                cs.student_id         AS student_id,
                ca.note               AS note_type,
                ca.weight             AS weight,
                COALESCE(caw.note, 0) AS note
            FROM
                estud.classes__students cs
            INNER JOIN
                estud.classes c ON c.id = cs.class_id
            INNER JOIN
                estud.class_activities ca ON ca.class_id = cs.class_id
            LEFT JOIN
                estud.class_activity_works caw ON caw.class_activity_id = ca.id AND caw.student_id = cs.student_id
            WHERE
                c.institution_id = {0}
                    AND
                c.period_id = {1}
                    AND
                cs.status = {2}
                    AND
                EXISTS (
                    SELECT 1
                    FROM estud.class_activities gca
                    INNER JOIN estud.class_activity_works gcaw ON gcaw.class_activity_id = gca.id
                    WHERE gca.class_id = c.id AND gcaw.status = {3}
                )
        ";

        return await ctx.Database
            .SqlQueryRaw<GetStudentsAtRiskWorkDto>(
                sql, institutionId, periodId, (int)StudentClassStatus.Matriculado, (int)ClassActivityWorkStatus.Finalized)
            .AsNoTracking().ToListAsync();
    }
}

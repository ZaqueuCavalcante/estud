using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Insights.GetLowestGradeClasses;

public class GetLowestGradeClassesService(EstudDbContext ctx) : IEstudService
{
    private const int Limit = 5;

    public async Task<OneOf<GetLowestGradeClassesOut, EstudError>> Get(GetLowestGradeClassesIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var periodExists = await ctx.AcademicPeriods.AnyAsync(p => p.Id == data.PeriodId && p.InstitutionId == institutionId);
        if (!periodExists) return AcademicPeriodNotFound.I;

        var config = await ctx.InstitutionConfigs.AsNoTracking().FirstAsync(x => x.InstitutionId == institutionId);

        var works = await GetWorks(institutionId, data.PeriodId);

        var averages = works
            .GroupBy(w => w.ClassId)
            .Select(c => new
            {
                ClassId = c.Key,
                Average = Math.Round(
                    c.GroupBy(w => w.StudentId)
                        .Average(s => Math.Round(config.GradeRule.Average(s.Select(w => (w.NoteType, w.Weight, w.Note))), 1, MidpointRounding.AwayFromZero)),
                    1, MidpointRounding.AwayFromZero),
            })
            .ToList();

        var classIds = averages.Select(x => x.ClassId).ToList();
        var disciplines = await ctx.Classes.AsNoTracking()
            .Where(c => classIds.Contains(c.Id))
            .Select(c => new { c.Id, Discipline = c.Discipline.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Discipline);

        return new GetLowestGradeClassesOut
        {
            Classes = averages
                .Select(x => new GetLowestGradeClassesItemOut
                {
                    Id = x.ClassId,
                    Discipline = disciplines[x.ClassId],
                    Average = x.Average,
                })
                .OrderBy(x => x.Average).ThenBy(x => x.Discipline).ThenBy(x => x.Id)
                .Take(Limit)
                .ToList(),
        };
    }

    private async Task<List<GetLowestGradeClassWorkDto>> GetWorks(int institutionId, int periodId)
    {
        const string sql = @"
            SELECT
                cs.class_id              AS class_id,
                cs.student_id            AS student_id,
                ca.note                  AS note_type,
                ca.weight                AS weight,
                COALESCE(caw.note, 0)    AS note
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
                EXISTS (
                    SELECT 1
                    FROM estud.class_activities gca
                    INNER JOIN estud.class_activity_works gcaw ON gcaw.class_activity_id = gca.id
                    WHERE gca.class_id = c.id AND gcaw.status = {2}
                )
        ";

        return await ctx.Database
            .SqlQueryRaw<GetLowestGradeClassWorkDto>(sql, institutionId, periodId, (int)ClassActivityWorkStatus.Finalized)
            .AsNoTracking().ToListAsync();
    }
}

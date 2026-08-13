using Estud.Back.Domain.Campi;
using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Campi.GetCampi;

public class GetCampiService(EstudDbContext ctx) : IEstudService
{
    public async Task<GetCampiOut> Get()
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var campi = await ctx.Campi.AsNoTracking()
            .Include(x => x.OpeningHours).Include(x => x.Classrooms)
            .Where(c => c.InstitutionId == institutionId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var classes = await GetClasses(institutionId);
        var schedules = await GetSchedules(institutionId);

        var items = campi.ConvertAll(x =>
        {
            var (usedMinutesRate, usedCapacityRate) = GetUsedRates(x, schedules, classes);
            return x.ToGetCampiItemOut(usedMinutesRate, usedCapacityRate);
        });

        return new GetCampiOut() { Total = items.Count, Items = items };
    }

    private static (decimal UsedMinutesRate, decimal UsedCapacityRate) GetUsedRates(
        Campus campus,
        List<Schedule> schedules,
        List<GetClassStudentsDto> classes)
    {
        var campusUsedMinutes = 0;
        var campusAvailableMinutes = 0;

        var campusUsedCapacity = 0;
        var campusAvailableCapacity = 0;

        foreach (var day in Day.All)
        {
            foreach (var shift in Shift.All)
            {
                // O que a célula tem de horário real: as janelas do dia recortadas ao turno.
                var openSchedules = campus.OpenSchedulesIn(day, shift);
                var campusOpenMinutes = campus.MinutesOpenIn(day, shift);

                foreach (var classroom in campus.Classrooms)
                {
                    var classroomSchedules = schedules.Where(x => x.ClassroomId == classroom.Id && x.Day == day).ToList();

                    foreach (var schedule in classroomSchedules)
                    {
                        var currentClass = classes.FirstOrDefault(c => c.Id == schedule.ClassId);
                        var currentClassStudents = currentClass?.Students ?? 0;

                        // Só conta o pedaço do horário que cai dentro de janela aberta do turno
                        var openSchedulesUsedMinutes = openSchedules
                            .Select(schedule.Intersect)
                            .OfType<Schedule>()
                            .Sum(s => s.GetDiffInMinutes());

                        campusUsedMinutes += openSchedulesUsedMinutes;
                        campusUsedCapacity += openSchedulesUsedMinutes * currentClassStudents;
                    }

                    campusAvailableMinutes += campusOpenMinutes;
                    campusAvailableCapacity += campusOpenMinutes * classroom.Capacity;
                }
            }
        }

        return new (ToRate(campusUsedMinutes, campusAvailableMinutes), ToRate(campusUsedCapacity, campusAvailableCapacity));
    }

    private async Task<List<Schedule>> GetSchedules(int institutionId)
    {
        const string sql = @"
            SELECT
                s.*
            FROM
                estud.classrooms cr
            INNER JOIN
                estud.schedules s ON s.classroom_id = cr.id
            INNER JOIN
                estud.classes c ON c.id = s.class_id
            WHERE
                cr.institution_id = {0}
                    AND
                c.status <> {1}
            GROUP BY
                s.id
        ";

        return await ctx.Schedules
            .FromSqlRaw(sql, institutionId, ClassStatus.Finalized.ToInt())
            .AsNoTracking().ToListAsync();
    }

    private async Task<List<GetClassStudentsDto>> GetClasses(int institutionId)
    {
        const string sql = @"
            SELECT
                c.id, count(s.student_id) AS students
            FROM
                estud.classes c
            INNER JOIN
                estud.classes__students s ON s.class_id = c.id
            WHERE
                c.institution_id = {0}
                    AND
                c.status <> {1}
                    AND
                s.status = {2}
            GROUP BY
                c.id
        ";

        return await ctx.Database
            .SqlQueryRaw<GetClassStudentsDto>(sql, institutionId, ClassStatus.Finalized.ToInt(), StudentClassStatus.Matriculado.ToInt())
            .AsNoTracking().ToListAsync();
    }

    private static decimal ToRate(int used, int available)
    {
        if (available <= 0) return 0M;
        return Math.Round(used * 100M / available, 2);
    }
}

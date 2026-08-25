using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Campi.GetCampusOccupancy;

public class GetCampusOccupancyService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetCampusOccupancyOut, EstudError>> Get(int campusId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var campus = await ctx.Campi.AsNoTracking()
            .Include(x => x.OpeningHours).Include(x => x.Classrooms.OrderBy(c => c.Name))
            .FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var classes = await GetClasses(institutionId);
        var schedules = await GetSchedules(institutionId, campusId);

        var cells = new List<CampusOccupancyCellOut>(Day.All.Length * Shift.All.Length);

        var campusUsedCapacity = 0L;
        var campusAvailableCapacity = 0L;
        var classroomTotals = campus.Classrooms.ToDictionary(c => c.Id, _ => new ClassroomTotals());

        foreach (var day in Day.All)
        {
            foreach (var shift in Shift.All)
            {
                // O que a célula tem de horário real: as janelas do dia recortadas ao turno.
                var openSchedules = campus.OpenSchedulesIn(day, shift);
                var campusOpenMinutes = campus.MinutesOpenIn(day, shift);

                var cellUsedCapacity = 0;
                var cellAvailableCapacity = 0;
                var cellClassrooms = new List<CampusOccupancyClassroomOut>(campus.Classrooms.Count);

                foreach (var classroom in campus.Classrooms)
                {
                    var classroomUsedMinutes = 0;
                    var classroomUsedCapacity = 0;
                    var classroomSchedules = schedules.Where(x => x.ClassroomId == classroom.Id && x.Day == day).ToList();

                    foreach (var schedule in classroomSchedules)
                    {
                        var currentClass = classes.FirstOrDefault(c => c.Id == schedule.ClassId);
                        var currentClassStudents = currentClass?.Students ?? 0;

                        // Só conta o pedaço do horário que cai dentro de janela aberta do turno
                        var usedMinutes = openSchedules
                            .Select(schedule.Intersect)
                            .OfType<Schedule>()
                            .Sum(s => s.GetDiffInMinutes());

                        classroomUsedMinutes += usedMinutes;
                        classroomUsedCapacity += usedMinutes * currentClassStudents;
                    }

                    cellUsedCapacity += classroomUsedCapacity;
                    cellAvailableCapacity += campusOpenMinutes * classroom.Capacity;

                    var totals = classroomTotals[classroom.Id];
                    totals.UsedMinutes += classroomUsedMinutes;
                    totals.UsedCapacity += classroomUsedCapacity;
                    totals.AvailableMinutes += campusOpenMinutes;

                    cellClassrooms.Add(new CampusOccupancyClassroomOut
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        UsedMinutes = classroomUsedMinutes,
                        AvailableMinutes = campusOpenMinutes,
                        UsedMinutesRate = ToRate(classroomUsedMinutes, campusOpenMinutes),
                        AverageStudents = ToAverageStudents(classroomUsedCapacity, campusOpenMinutes),
                        UsedCapacityRate = ToRate(classroomUsedCapacity, campusOpenMinutes * classroom.Capacity),
                    });
                }

                campusUsedCapacity += cellUsedCapacity;
                campusAvailableCapacity += cellAvailableCapacity;

                var used = cellClassrooms.Sum(c => c.UsedMinutes);
                var available = cellClassrooms.Sum(c => c.AvailableMinutes);

                cells.Add(new CampusOccupancyCellOut
                {
                    Day = day,
                    Shift = shift,
                    UsedMinutes = used,
                    Classrooms = cellClassrooms,
                    AvailableMinutes = available,
                    OpenMinutes = campusOpenMinutes,
                    Open = campusOpenMinutes > 0,
                    UsedCapacity = cellUsedCapacity,
                    UsedMinutesRate = ToRate(used, available),
                    UsedCapacityRate = ToRate(cellUsedCapacity, cellAvailableCapacity),
                });
            }
        }

        var campusUsedMinutes = cells.Sum(c => c.UsedMinutes);
        var campusAvailableMinutes = cells.Sum(c => c.AvailableMinutes);

        var classrooms = campus.Classrooms
            .Select(classroom =>
            {
                var totals = classroomTotals[classroom.Id];
                return new CampusClassroomOccupancyOut
                {
                    Id = classroom.Id,
                    Name = classroom.Name,
                    Capacity = classroom.Capacity,
                    UsedMinutes = totals.UsedMinutes,
                    UsedCapacity = totals.UsedCapacity,
                    AvailableMinutes = totals.AvailableMinutes,
                    UsedMinutesRate = ToRate(totals.UsedMinutes, totals.AvailableMinutes),
                    AverageStudents = ToAverageStudents(totals.UsedCapacity, totals.AvailableMinutes),
                    UsedCapacityRate = ToRate(totals.UsedCapacity, totals.AvailableMinutes * classroom.Capacity),
                };
            })
            .ToList();

        return new GetCampusOccupancyOut
        {
            Cells = cells,
            CampusId = campus.Id,
            Campus = campus.Name,
            Classrooms = classrooms,
            OpenCells = cells.Count(x => x.Open),
            TotalClassrooms = campus.Classrooms.Count,
            OverallUsedMinutesRate = ToRate(campusUsedMinutes, campusAvailableMinutes),
            OverallUsedCapacityRate = ToRate(campusUsedCapacity, campusAvailableCapacity),
        };
    }

    private async Task<List<Schedule>> GetSchedules(int institutionId, int campusId)
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
                cr.campus_id = {1}
                    AND
                c.status <> {2}
            GROUP BY
                s.id
        ";

        return await ctx.Schedules
            .FromSqlRaw(sql, institutionId, campusId, ClassStatus.Finalized.ToInt())
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

    private static int ToAverageStudents(int usedCapacity, int availableMinutes)
    {
        if (usedCapacity <= 0 || availableMinutes <= 0) return 0;

        // Sala com qualquer movimento nunca zera: meia turma em média ainda é gente na sala.
        var average = usedCapacity / (decimal) availableMinutes;
        return Math.Max((int) Math.Floor(average), 1);
    }

    private static decimal ToRate(long used, long available)
    {
        if (available <= 0) return 0M;
        return Math.Round(used * 100M / available, 2);
    }
}

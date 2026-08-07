using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Campi.GetCampusOccupancy;

public class GetCampusOccupancyService(EstudDbContext ctx) : IEstudService
{
    private static readonly Day[] Days = Enum.GetValues<Day>();
    private static readonly Shift[] Shifts = Enum.GetValues<Shift>();

    public async Task<OneOf<GetCampusOccupancyOut, EstudError>> Get(int campusId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var campus = await ctx.Campi.AsNoTracking()
            .Include(x => x.OpeningHours).Include(x => x.Classrooms)
            .FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var classes = await GetClasses(institutionId);
        var schedules = await GetSchedules(institutionId, campusId);

        var openCells = 0;
        var cells = new List<CampusOccupancyCellOut>(Days.Length * Shifts.Length);

        foreach (var day in Days)
        {
            foreach (var shift in Shifts)
            {
                var campusOpenMinutes = campus.MinutesOpenIn(day, shift);
                if (campusOpenMinutes > 0) openCells++;

                var cellClassrooms = new List<CampusOccupancyClassroomOut>(campus.Classrooms.Count);

                foreach (var classroom in campus.Classrooms)
                {
                    var classroomUsedMinutes = 0;
                    var classroomAvailableMinutes = campusOpenMinutes * classroom.Capacity;

                    var classroomSchedules = schedules.Where(x => x.ClassroomId == classroom.Id && x.Day == day &&
                        x.Start >= shift.StartAtHour && x.End <= shift.EndAtHour).ToList();

                    foreach (var schedule in classroomSchedules)
                    {
                        var currentClass = classes.FirstOrDefault(c => c.Id == schedule.ClassId);
                        if (currentClass == null) continue;

                        classroomUsedMinutes += schedule.GetDiffInMinutes() * currentClass.Students;
                    }

                    cellClassrooms.Add(new CampusOccupancyClassroomOut
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        UsedMinutes = classroomUsedMinutes,
                        AvailableMinutes = classroomAvailableMinutes,
                        Rate = ToRate(classroomUsedMinutes, classroomAvailableMinutes),
                    });
                }

                var used = cellClassrooms.Sum(c => c.UsedMinutes);
                var available = cellClassrooms.Sum(c => c.AvailableMinutes);

                cells.Add(new CampusOccupancyCellOut
                {
                    Day = day,
                    Shift = shift,
                    UsedMinutes = used,
                    Classrooms = cellClassrooms,
                    AvailableMinutes = available,
                    Open = campusOpenMinutes > 0,
                    Rate = ToRate(used, available),
                });
            }
        }

        var usedSeatMinutes = cells.Sum(c => c.UsedMinutes);
        var availableSeatMinutes = cells.Sum(c => c.AvailableMinutes);

        return new GetCampusOccupancyOut
        {
            Cells = cells,
            CampusId = campus.Id,
            Campus = campus.Name,
            OpenCells = openCells,
            TotalClassrooms = campus.Classrooms.Count,
            OverallRate = ToRate(usedSeatMinutes, availableSeatMinutes),
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

    private async Task<List<GetClassDto>> GetClasses(int institutionId)
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
            .SqlQueryRaw<GetClassDto>(sql, institutionId, ClassStatus.Finalized.ToInt(), StudentClassStatus.Matriculado.ToInt())
            .AsNoTracking().ToListAsync();
    }

    private static decimal ToRate(int used, int available)
    {
        if (available <= 0) return 0M;
        return Math.Round(used * 100M / available, 2);
    }
}

using Estud.Back.Domain.Campi;
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
            .Include(x => x.Classrooms).Include(x => x.OpeningHours)
            .FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var classes = await GetClasses(institutionId);
        var schedules = await GetSchedules(institutionId, campusId);

        // Classroom + Day -> Schedules
        var schedulesByClassroomDay = schedules
            .GroupBy(s => (ClassroomId: s.ClassroomId!.Value, s.Day))
            .ToDictionary(g => g.Key, g => g.ToList());

        var openCells = 0;
        var usedSeatMinutes = 0;
        var availableSeatMinutes = 0;
        var totalCapacity = campus.Classrooms.Sum(c => c.Capacity);
        var openingHours = new WeeklyOpeningHours(campus.OpeningHours);
        var cells = new List<CampusOccupancyCellOut>(Days.Length * Shifts.Length);

        foreach (var day in Days)
        {
            foreach (var shift in Shifts)
            {
                // O teto da célula é quanto o campus abre dentro do turno, e não a duração do turno.
                var openMinutes = openingHours.MinutesOpenIn(day, shift);
                if (openMinutes > 0) openCells++;

                var cellClassrooms = new List<CampusOccupancyClassroomOut>(campus.Classrooms.Count);

                foreach (var classroom in campus.Classrooms)
                {
                    var classroomUsed = 0;

                    if (schedulesByClassroomDay.TryGetValue((classroom.Id, day), out var daySchedules))
                    {
                        foreach (var schedule in daySchedules)
                        {
                            var minutes = UsedInMinutes(schedule, shift, day, openingHours);
                            classroomUsed += minutes;

                            // Sala alocada não é sala cheia: na ocupação geral o que conta é o
                            // assento ocupado. Uma sala de 6 lugares com uma turma de 3 alunos
                            // está pela metade, mesmo com o horário inteiro tomado.
                            var studentsInSchedule = classes.FirstOrDefault(c => c.Id == schedule.ClassId)?.Students ?? 0;
                            usedSeatMinutes += minutes * studentsInSchedule;
                        }
                    }

                    cellClassrooms.Add(new CampusOccupancyClassroomOut
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        UsedMinutes = classroomUsed,
                        Rate = ToRate(classroomUsed, openMinutes),
                    });
                }

                var available = campus.Classrooms.Count * openMinutes;
                var used = cellClassrooms.Sum(c => c.UsedMinutes);

                availableSeatMinutes += totalCapacity * openMinutes;

                cells.Add(new CampusOccupancyCellOut
                {
                    Day = day,
                    Shift = shift,
                    UsedMinutes = used,
                    Open = openMinutes > 0,
                    Classrooms = cellClassrooms,
                    AvailableMinutes = available,
                    Rate = ToRate(used, available),
                });
            }
        }

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

    // Quanto do horário cai dentro da janela do turno e, ao mesmo tempo, dentro do
    // funcionamento do campus. Um horário 10h–14h conta 120min na manhã e 120min na
    // tarde; um 06h–07h não conta em lugar nenhum; e um 06h–08h num campus que abre
    // às 07h conta 60min — o resto é aula num horário que o campus não tem.
    private static int UsedInMinutes(Schedule schedule, Shift shift, Day day, WeeklyOpeningHours openingHours)
    {
        var start = Math.Max(schedule.Start.ToMinutes(), shift.StartInMinutes);
        var end = Math.Min(schedule.End.ToMinutes(), shift.EndInMinutes);
        if (end <= start) return 0;

        return openingHours.ClipToOpen(day, start, end);
    }

    private static decimal ToRate(int used, int available)
    {
        if (available <= 0) return 0M;
        return Math.Round(used * 100M / available, 2);
    }
}

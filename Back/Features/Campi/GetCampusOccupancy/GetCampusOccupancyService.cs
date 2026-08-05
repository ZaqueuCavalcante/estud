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

        var campus = await ctx.Campi.Include(x => x.OpeningHours).AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var classrooms = await ctx.Classrooms.AsNoTracking()
            .Where(c => c.CampusId == campusId && c.InstitutionId == institutionId)
            .OrderBy(c => c.Name).ToListAsync();

        var classes = await ctx.Classes.AsNoTracking().Include(x => x.Schedules)
            .Where(x => x.InstitutionId == institutionId && x.Status != ClassStatus.Finalized).ToListAsync();

        // Turma online tem ClassroomId nulo, então já cai fora aqui.
        var classroomIds = classrooms.Select(c => c.Id).ToHashSet();
        var schedules = classes.SelectMany(x => x.Schedules)
            .Where(x => x.ClassroomId != null && classroomIds.Contains(x.ClassroomId.Value)).ToList();

        // O mapa é varrido por sala x dia, então o índice segue esse par.
        var schedulesByClassroomDay = schedules
            .GroupBy(s => (ClassroomId: s.ClassroomId!.Value, s.Day))
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalUsed = 0;
        var openCells = 0;
        var totalAvailable = 0;
        var openingHours = new WeeklyOpeningHours(campus.OpeningHours);
        var cells = new List<CampusOccupancyCellOut>(Days.Length * Shifts.Length);

        foreach (var day in Days)
        {
            foreach (var shift in Shifts)
            {
                // O teto da célula é quanto o campus abre dentro do turno, e não a duração do turno.
                var openMinutes = openingHours.MinutesOpenIn(day, shift);
                var open = openMinutes > 0;
                if (open) openCells++;

                var cellClassrooms = classrooms.ConvertAll(classroom =>
                {
                    var classroomUsed = schedulesByClassroomDay.TryGetValue((classroom.Id, day), out var daySchedules)
                        ? daySchedules.Sum(s => UsedInMinutes(s, shift, day, openingHours))
                        : 0;

                    return new CampusOccupancyClassroomOut
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        UsedMinutes = classroomUsed,
                        Rate = ToRate(classroomUsed, openMinutes),
                    };
                });

                var available = classrooms.Count * openMinutes;
                var used = cellClassrooms.Sum(c => c.UsedMinutes);

                totalUsed += used;
                totalAvailable += available;

                cells.Add(new CampusOccupancyCellOut
                {
                    Day = day,
                    Open = open,
                    Shift = shift,
                    UsedMinutes = used,
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
            TotalClassrooms = classrooms.Count,
            OverallRate = ToRate(totalUsed, totalAvailable),
        };
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

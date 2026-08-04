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

        var campus = await ctx.Campi.AsNoTracking().FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var hours = await ctx.OpeningHours.AsNoTracking().Where(o => o.CampusId == campusId).ToListAsync();
        var openingHours = new WeeklyOpeningHours(hours);

        var classrooms = await ctx.Classrooms.AsNoTracking()
            .Where(c => c.CampusId == campusId && c.InstitutionId == institutionId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var classes = await ctx.Classes.AsNoTracking()
            .Include(x => x.Schedules)
            .Where(x => x.InstitutionId == institutionId && x.Status != ClassStatus.Finalized)
            .ToListAsync();

        var classroomIds = classrooms.Select(c => c.Id).ToHashSet();

        // Turma online tem ClassroomId nulo, então já cai fora aqui.
        var schedules = classes.SelectMany(x => x.Schedules)
            .Where(x => x.ClassroomId != null && classroomIds.Contains(x.ClassroomId.Value)).ToList();

        // O mapa é varrido por sala x dia, então o índice segue esse par.
        var schedulesByClassroomDay = schedules
            .GroupBy(s => (ClassroomId: s.ClassroomId!.Value, s.Day))
            .ToDictionary(g => g.Key, g => g.ToList());

        var cells = new List<CampusOccupancyCellOut>(Days.Length * Shifts.Length);
        var totalUsed = 0;
        var totalAvailable = 0;
        var openCells = 0;

        foreach (var day in Days)
        {
            foreach (var shift in Shifts)
            {
                // O teto da célula é quanto o campus abre dentro do turno, e não a
                // duração do turno: campus que fecha às 22h tem 240min de noite, e
                // campus fechado no sábado tem 0 — célula fechada, não célula vazia.
                var openMinutes = openingHours.MinutesOpenIn(day, shift);
                var open = openMinutes > 0;
                if (open) openCells++;

                var cellClassrooms = classrooms.ConvertAll(classroom =>
                {
                    var classroomUsed = schedulesByClassroomDay.TryGetValue((classroom.Id, day), out var daySchedules)
                        ? daySchedules.Sum(s => UsedInMinutes(s, shift, day, openingHours))
                        : 0;

                    // Duas turmas na mesma sala e horário não deveriam existir
                    // (ClassroomScheduleConflict barra na alocação), mas o teto
                    // evita uma sala aparecer com 130% caso escape alguma.
                    classroomUsed = Math.Min(classroomUsed, openMinutes);

                    return new CampusOccupancyClassroomOut
                    {
                        Id = classroom.Id,
                        Name = classroom.Name,
                        UsedMinutes = classroomUsed,
                        Rate = ToRate(classroomUsed, openMinutes),
                    };
                });

                var used = cellClassrooms.Sum(c => c.UsedMinutes);
                var available = classrooms.Count * openMinutes;

                totalUsed += used;
                totalAvailable += available;

                cells.Add(new CampusOccupancyCellOut
                {
                    Day = day,
                    Shift = shift,
                    Open = open,
                    UsedMinutes = used,
                    AvailableMinutes = available,
                    Rate = ToRate(used, available),
                    Classrooms = cellClassrooms,
                });
            }
        }

        return new GetCampusOccupancyOut
        {
            CampusId = campus.Id,
            Campus = campus.Name,
            TotalClassrooms = classrooms.Count,
            OverallRate = ToRate(totalUsed, totalAvailable),
            OpenCells = openCells,
            Cells = cells,
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

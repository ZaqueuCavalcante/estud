namespace Estud.Back.Features.Campi.GetCampusOpeningHours;

public class GetCampusOpeningHoursService(EstudDbContext ctx) : IEstudService
{
    private static readonly Day[] Days = Enum.GetValues<Day>();

    public async Task<OneOf<GetCampusOpeningHoursOut, EstudError>> Get(int campusId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var campus = await ctx.Campi.AsNoTracking().FirstOrDefaultAsync(c => c.Id == campusId && c.InstitutionId == institutionId);
        if (campus == null) return CampusNotFound.I;

        var hours = await ctx.OpeningHours.AsNoTracking().Where(o => o.CampusId == campusId).ToListAsync();

        // A UI desenha uma linha por dia, então os 6 dias vêm sempre — dia sem
        // linha no banco vira Windows vazia, que é como "fechado" se expressa.
        var days = Days.Select(day => new CampusOpeningHoursDayOut
        {
            Day = day,
            Windows = hours
                .Where(h => h.Day == day)
                .OrderBy(h => h.Start)
                .Select(h => new CampusOpeningHoursWindowOut { Start = h.Start, End = h.End })
                .ToList(),
        }).ToList();

        return new GetCampusOpeningHoursOut
        {
            CampusId = campus.Id,
            Campus = campus.Name,
            Days = days,
        };
    }
}

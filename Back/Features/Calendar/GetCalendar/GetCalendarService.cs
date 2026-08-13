namespace Estud.Back.Features.Calendar.GetCalendar;

public class GetCalendarService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetCalendarOut, EstudError>> Get(GetCalendarIn data)
    {
        var year = data.Year ?? DateTime.UtcNow.Year;
        var campusId = data.CampusId;

        string? campusName = null;
        if (campusId != null)
        {
            campusName = await ctx.Campi.AsNoTracking()
                .Where(c => c.Id == campusId && c.InstitutionId == ctx.RequestUser.InstitutionId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();

            if (campusName == null) return CampusNotFound.I;
        }

        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        var calendar = await ctx.GetCalendarResolver(campusId, start, end);

        var items = new List<GetCalendarItemOut>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var day = calendar.Resolve(date);
            items.Add(day.ToGetCalendarItemOut(calendar.OverrideIdIn(date, campusScope: campusId != null)));
        }

        return new GetCalendarOut
        {
            Year = year,
            CampusId = campusId,
            Campus = campusName,
            Total = items.Count,
            Items = items,
        };
    }
}

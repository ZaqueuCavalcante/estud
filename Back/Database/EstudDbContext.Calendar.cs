using Estud.Back.Domain.Calendar;
using Estud.Back.Database.Calendar;

namespace Estud.Back.Database;

public partial class EstudDbContext
{
    public DbSet<CalendarDay> CalendarDays { get; set; }

    private static void ConfigureCalendar(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CalendarDayDbConfig());
    }

    /// <summary>
    /// Monta o resolver do calendário para um escopo e intervalo.
    /// </summary>
    public async Task<CalendarResolver> GetCalendarResolver(int? campusId, DateOnly start, DateOnly end)
    {
        var institutionId = RequestUser.InstitutionId;

        var days = await CalendarDays.AsNoTracking()
            .Where(d => d.InstitutionId == institutionId)
            .Where(d => d.CampusId == null || d.CampusId == campusId)
            .Where(d => d.Date >= start && d.Date <= end)
            .ToListAsync();

        return new CalendarResolver(days);
    }
}

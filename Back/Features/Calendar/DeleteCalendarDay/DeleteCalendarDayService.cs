namespace Estud.Back.Features.Calendar.DeleteCalendarDay;

public class DeleteCalendarDayService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Delete(int dayId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var day = await ctx.CalendarDays.FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == dayId);
        if (day == null) return CalendarDayNotFound.I;

        ctx.Remove(day);
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

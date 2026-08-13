using Estud.Back.Domain.Calendar;

namespace Estud.Back.Features.Calendar.GetCalendar;

public static class GetCalendarMapper
{
    extension(ResolvedCalendarDay day)
    {
        public GetCalendarItemOut ToGetCalendarItemOut(int? overrideId)
        {
            return new()
            {
                Id = overrideId,
                Source = day.Source,
                DayType = day.DayType,
                Description = day.Description,
                Date = day.Date.ToDateTime(TimeOnly.MinValue),
            };
        }
    }
}

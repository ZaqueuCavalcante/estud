using Estud.Back.Domain.Calendar;

namespace Estud.Back.Features.Calendar.UpdateCalendarDay;

public static class UpdateCalendarDayMapper
{
    extension(CalendarDay day)
    {
        public UpdateCalendarDayOut ToUpdateCalendarDayOut()
        {
            return new()
            {
                Id = day.Id,
                CampusId = day.CampusId,
                Date = day.Date.ToDateTime(TimeOnly.MinValue),
                DayType = day.DayType,
                Description = day.Description,
            };
        }
    }
}

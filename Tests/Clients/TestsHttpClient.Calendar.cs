using System.Net.Http.Json;
using Estud.Back.Features.Calendar.GetCalendar;
using Estud.Back.Features.Calendar.CreateCalendarDay;
using Estud.Back.Features.Calendar.UpdateCalendarDay;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<GetCalendarOut, ErrorOut>> GetCalendar(int? year = null, int? campusId = null)
    {
        var data = new GetCalendarIn { Year = year, CampusId = campusId };
        var response = await http.GetAsync("calendar".AddQueryString(data));
        return await response.Resolve<GetCalendarOut>();
    }

    public async Task<OneOf<CreateCalendarDayOut, ErrorOut>> CreateCalendarDay(
        DateTime? date = null,
        DayType? dayType = DayType.Vacation,
        string? description = "Férias de verão",
        DateTime? endDate = null,
        int? campusId = null
    ) {
        var data = new CreateCalendarDayIn
        {
            Date = date ?? new DateTime(2026, 1, 5),
            EndDate = endDate,
            CampusId = campusId,
            DayType = dayType,
            Description = description,
        };
        var response = await http.PostAsJsonAsync("calendar/days", data);
        return await response.Resolve<CreateCalendarDayOut>();
    }

    public async Task<OneOf<UpdateCalendarDayOut, ErrorOut>> UpdateCalendarDay(
        int dayId,
        DayType? dayType = DayType.Recess,
        string? description = "Recesso de fim de ano"
    ) {
        var data = new UpdateCalendarDayIn
        {
            DayType = dayType,
            Description = description,
        };
        var response = await http.PutAsJsonAsync($"calendar/days/{dayId}", data);
        return await response.Resolve<UpdateCalendarDayOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> DeleteCalendarDay(int dayId)
    {
        var response = await http.DeleteAsync($"calendar/days/{dayId}");
        return await response.Resolve<SuccessOut>();
    }
}

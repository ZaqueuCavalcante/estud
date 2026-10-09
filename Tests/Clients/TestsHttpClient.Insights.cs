using Estud.Back.Features.Insights.GetAttendance;
using Estud.Back.Features.Insights.GetPendingAttendance;
using Estud.Back.Features.Insights.GetLowestAttendanceClasses;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<GetAttendanceOut, ErrorOut>> GetAttendance(int periodId)
    {
        var data = new GetAttendanceIn { PeriodId = periodId };
        var response = await http.GetAsync("/insights/attendance".AddQueryString(data));
        return await response.Resolve<GetAttendanceOut>();
    }

    public async Task<OneOf<GetLowestAttendanceClassesOut, ErrorOut>> GetLowestAttendanceClasses(int periodId)
    {
        var data = new GetLowestAttendanceClassesIn { PeriodId = periodId };
        var response = await http.GetAsync("/insights/classes/lowest-attendance".AddQueryString(data));
        return await response.Resolve<GetLowestAttendanceClassesOut>();
    }

    public async Task<OneOf<GetPendingAttendanceOut, ErrorOut>> GetPendingAttendance(int periodId)
    {
        var data = new GetPendingAttendanceIn { PeriodId = periodId };
        var response = await http.GetAsync("/insights/classes/pending-attendance".AddQueryString(data));
        return await response.Resolve<GetPendingAttendanceOut>();
    }
}

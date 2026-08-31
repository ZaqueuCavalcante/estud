using Estud.Tests.Integration.Clients;
using Estud.Back.Features.Periods.GetAcademicPeriods;

namespace Estud.Tests.Shortcuts;

public static class TestShortcuts
{
    public static async Task<GetAcademicPeriodsItemOut> GetFirstAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.First();
    }

    public static async Task<GetAcademicPeriodsItemOut> GetLastAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.Last();
    }

    public static async Task<List<int>> GetClassLessons(this TestsHttpClient client, int classId)
    {
        var lessons = await client.GetTeacherClassLessons(classId).Success();

        return lessons.Lessons.Select(x => x.Id).ToList();
    }
}

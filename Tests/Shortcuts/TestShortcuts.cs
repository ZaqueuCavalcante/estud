using Estud.Tests.Integration.Clients;
using Estud.Back.Features.Periods.GetAcademicPeriods;

namespace Estud.Tests.Shortcuts;

public static class TestShortcuts
{
    public static async Task<GetAcademicPeriodsItemOut> GetFirstAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.First();
    }
}

using Quartz;
using Estud.Back.Sso;

namespace Estud.Back.Extensions;

public static class SsoDomainsExtensions
{
    extension(IScheduler scheduler)
    {
        public async Task TriggerSsoDomainsVerificationProcessorJob()
        {
            await scheduler.TriggerJob(new JobKey(nameof(SsoDomainsVerificationProcessor)));
        }
    }
}

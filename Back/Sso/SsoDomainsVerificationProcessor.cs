using Quartz;

namespace Estud.Back.Sso;

/// <summary>
/// Re-checks the DNS TXT record of the SSO domains that are already verified. Verifying once and
/// never again would leave an expired domain — bought by someone else — still routing logins to
/// the old institution.
/// </summary>
[DisallowConcurrentExecution]
public class SsoDomainsVerificationProcessor(IServiceScopeFactory serviceScopeFactory, SsoSettings settings) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var ctx = scope.ServiceProvider.GetRequiredService<EstudDbContext>();
        var verifier = scope.ServiceProvider.GetRequiredService<SsoDomainVerifier>();

        var threshold = DateTime.UtcNow.AddHours(-settings.DomainRecheckIntervalInHours);

        // Only domains that already proved ownership are re-checked. A pending setup is advanced by
        // the explicit verify endpoint, so a tenant that never publishes the record is not retried forever.
        var domains = await ctx.WebSsoAllowedDomains
            .Where(d => d.Status == SsoDomainStatus.Verified || d.Status == SsoDomainStatus.Expired)
            .Where(d => d.LastCheckedAt == null || d.LastCheckedAt < threshold)
            .OrderBy(d => d.LastCheckedAt)
            .Take(100)
            .ToListAsync();

        foreach (var domain in domains)
        {
            await verifier.Verify(domain);
        }

        await ctx.SaveChangesAsync();
    }
}

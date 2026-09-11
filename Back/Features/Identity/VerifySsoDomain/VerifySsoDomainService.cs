using Estud.Back.Auth.Managers;

namespace Estud.Back.Features.Identity.VerifySsoDomain;

public class VerifySsoDomainService(EstudDbContext ctx, DnsManager dns) : IEstudService
{
    private class Validator : AbstractValidator<VerifySsoDomainIn>
    {
        public Validator()
        {
            RuleFor(x => x.Domain).NotEmpty().WithError(InvalidSsoDomain.I);
            RuleFor(x => x.Domain).Must(x => x.NormalizeSsoDomain() != null).WithError(InvalidSsoDomain.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<VerifySsoDomainOut, EstudError>> Verify(Guid ssoConfigurationId, VerifySsoDomainIn data)
    {
        if (V.Run(data, out var error)) return error;

        var config = await ctx.WebSsoConfigurations
            .Include(x => x.AllowedDomains)
            .Where(x => x.PublicId == ssoConfigurationId && x.InstitutionId == ctx.RequestUser.InstitutionId)
            .FirstOrDefaultAsync();

        if (config == null) return SsoConfigurationNotFound.I;

        var name = data.Domain.NormalizeSsoDomain();
        var domain = config.AllowedDomains.FirstOrDefault(d => d.Domain == name);
        if (domain == null) return SsoDomainNotFound.I;

        if (domain.Status == SsoDomainStatus.Verified) return domain.ToVerifySsoDomainOut();

        var verifiedElsewhere = await ctx.WebSsoAllowedDomains
            .AnyAsync(d => d.Domain == domain.Domain && d.Status == SsoDomainStatus.Verified);
        if (verifiedElsewhere) return SsoDomainVerifiedByAnotherInstitution.I;

        var records = await dns.GetTxtRecords(domain.TxtRecordName);
        if (records == null) return SsoDomainDnsLookupFailed.I;

        if (!records.Any(r => r.Trim() == domain.TxtRecordValue)) return SsoDomainVerificationRecordNotFound.I;

        domain.MarkAsVerified();
        await ctx.SaveChangesAsync();

        return domain.ToVerifySsoDomainOut();
    }
}

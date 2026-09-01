using Estud.Back.Sso;

namespace Estud.Back.Features.Identity.VerifySsoDomain;

public class VerifySsoDomainService(EstudDbContext ctx, SsoDomainVerifier verifier) : IEstudService
{
    public async Task<OneOf<VerifySsoDomainOut, EstudError>> Verify(string domain)
    {
        var normalized = domain.NormalizeSsoDomain();
        if (normalized == null) return SsoDomainNotFound.I;

        var institutionId = ctx.RequestUser.InstitutionId;

        var allowedDomain = await ctx.WebSsoAllowedDomains
            .Where(d => d.Domain == normalized && d.Configuration!.InstitutionId == institutionId)
            .FirstOrDefaultAsync();

        if (allowedDomain == null) return SsoDomainNotFound.I;

        var verified = await verifier.Verify(allowedDomain);

        await ctx.SaveChangesAsync();

        if (!verified) return SsoDomainVerificationFailed.I;

        return new VerifySsoDomainOut
        {
            Domain = allowedDomain.Domain,
            Status = allowedDomain.Status,
            VerifiedAt = allowedDomain.VerifiedAt,
        };
    }
}

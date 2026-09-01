using Estud.Back.Auth.Schemes;

namespace Estud.Back.Features.Identity.SsoChallenge;

public class SsoChallengeService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<string, EstudError>> GetScheme(string? email)
    {
        if (email.IsEmpty() || !email!.IsValidEmail()) return InvalidEmail.I;

        var domain = email!.Split('@').Last().ToLowerInvariant();

        var publicId = await ctx.WebSsoConfigurations
            .Where(x => x.IsActive && x.AllowedDomains.Any(d => d.Domain == domain && d.Status == SsoDomainStatus.Verified))
            .Select(x => x.PublicId)
            .FirstOrDefaultAsync();

        if (publicId == Guid.Empty) return SsoNotConfiguredForDomain.I;

        return $"{SsoOidcScheme.Prefix}{publicId}";
    }
}

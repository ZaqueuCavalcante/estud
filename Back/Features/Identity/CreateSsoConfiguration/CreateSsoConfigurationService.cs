using Estud.Back.Auth.Managers;
using Estud.Back.Domain.Identity;

namespace Estud.Back.Features.Identity.CreateSsoConfiguration;

public class CreateSsoConfigurationService(EstudDbContext ctx, SsoEncryptionManager encryption, SsoSchemeManager ssoSchemeManager) : IEstudService
{
    private class Validator : AbstractValidator<CreateSsoConfigurationIn>
    {
        public Validator()
        {
            RuleFor(x => x.ProviderType).IsInEnum().WithError(InvalidSsoProviderType.I);

            RuleFor(x => x.Authority).NotEmpty().WithError(InvalidSsoAuthority.I);

            RuleFor(x => x.ClientId).NotEmpty().WithError(InvalidSsoClientId.I);
            RuleFor(x => x.ClientId).MinimumLength(5).WithError(InvalidSsoClientId.I);

            RuleFor(x => x.ClientSecret).NotEmpty().WithError(InvalidSsoClientSecret.I);
            RuleFor(x => x.ClientSecret).MinimumLength(10).WithError(InvalidSsoClientSecret.I);

            RuleFor(x => x.Domain).NotEmpty().WithError(InvalidSsoDomain.I);
            RuleFor(x => x.Domain).Must(x => x.NormalizeSsoDomain() != null).WithError(InvalidSsoDomain.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateSsoConfigurationOut, EstudError>> Create(CreateSsoConfigurationIn data)
    {
        if (V.Run(data, out var error)) return error;

        var institutionId = ctx.RequestUser.InstitutionId;

        var authorityError = data.Authority.ValidateSsoAuthority();
        if (authorityError != null) return authorityError;

        var domain = data.Domain.NormalizeSsoDomain()!;
        if (domain.IsPublicEmailDomain()) return SsoPublicDomainNotAllowed.I;

        var domainTaken = await ctx.WebSsoAllowedDomains.AnyAsync(d => d.Domain == domain
            && (d.Status == SsoDomainStatus.Verified || d.SsoConfiguration!.InstitutionId == institutionId));
        if (domainTaken) return SsoDomainAlreadyConfigured.I;

        var config = new SsoConfiguration(
            institutionId,
            data.ProviderType,
            data.Authority.TrimEnd('/'),
            data.ClientId.Trim(),
            encryption.Encrypt(data.ClientSecret),
            [domain],
            data.RequireSso);

        await ctx.SaveChangesAsync(config);

        ssoSchemeManager.RegisterScheme(config);

        return new CreateSsoConfigurationOut { Id = config.PublicId };
    }
}

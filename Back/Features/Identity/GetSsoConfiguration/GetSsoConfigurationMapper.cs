using Estud.Back.Domain.Identity;

namespace Estud.Back.Features.Identity.GetSsoConfiguration;

public static class GetSsoConfigurationMapper
{
    extension(SsoConfiguration config)
    {
        public GetSsoConfigurationOut ToGetSsoConfigurationOut()
        {
            return new()
            {
                Id = config.PublicId,
                ProviderType = config.ProviderType,
                Authority = config.Authority,
                ClientId = config.ClientId,
                IsActive = config.IsActive,
                RequireSso = config.RequireSso,
                CreatedAt = config.CreatedAt,
                Domains = config.AllowedDomains
                    .OrderBy(d => d.Domain)
                    .Select(d => d.ToGetSsoConfigurationDomainOut())
                    .ToList(),
            };
        }
    }

    extension(SsoAllowedDomain domain)
    {
        public GetSsoConfigurationDomainOut ToGetSsoConfigurationDomainOut()
        {
            return new()
            {
                Domain = domain.Domain,
                Status = domain.Status,
                VerifiedAt = domain.VerifiedAt,
                TxtRecordName = domain.TxtRecordName,
                TxtRecordValue = domain.TxtRecordValue,
            };
        }
    }
}

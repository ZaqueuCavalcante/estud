using Estud.Back.Domain.Identity;

namespace Estud.Back.Features.Identity.VerifySsoDomain;

public static class VerifySsoDomainMapper
{
    extension(SsoAllowedDomain domain)
    {
        public VerifySsoDomainOut ToVerifySsoDomainOut()
        {
            return new()
            {
                Domain = domain.Domain,
                Status = domain.Status,
                VerifiedAt = domain.VerifiedAt,
            };
        }
    }
}

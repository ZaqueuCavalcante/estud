using Estud.Back.Domain.Identity;

namespace Estud.Back.Sso;

public class SsoDomainVerifier(DnsTxtResolver resolver)
{
    public async Task<bool> Verify(SsoAllowedDomain domain)
    {
        var lookup = await resolver.ResolveTxt(domain.RecordName);

        if (lookup.Failed)
        {
            domain.Fail(lookup.Error!);
            return false;
        }

        if (!domain.Matches(lookup.Records))
        {
            domain.Fail(SsoDomainVerificationFailed.I.Message);
            return false;
        }

        domain.Verify();
        return true;
    }
}

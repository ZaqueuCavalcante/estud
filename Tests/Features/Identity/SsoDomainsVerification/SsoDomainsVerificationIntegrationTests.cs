namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Identity_SsoDomainsVerification_Should_expire_verified_domain_when_txt_record_is_gone()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-recheck-expires.com");
        var created = await client.CreateSsoConfiguration().Success();

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await client.VerifySsoDomain(created.Domain).Success();

        await _mocks.SetDnsTxtRecord(created.RecordName);
        await AgeSsoDomainCheck(created.Domain);

        // Act
        await _back.AwaitSsoDomainsVerification();

        // Assert
        var config = await client.GetSsoConfiguration().Success();
        config.Domains.Single().Status.Should().Be(SsoDomainStatus.Expired);
    }

    [Test]
    public async Task Identity_SsoDomainsVerification_Should_restore_expired_domain_when_txt_record_comes_back()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-recheck-restores.com");
        var created = await client.CreateSsoConfiguration().Success();

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await client.VerifySsoDomain(created.Domain).Success();

        await _mocks.SetDnsTxtRecord(created.RecordName);
        await AgeSsoDomainCheck(created.Domain);
        await _back.AwaitSsoDomainsVerification();

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await AgeSsoDomainCheck(created.Domain);

        // Act
        await _back.AwaitSsoDomainsVerification();

        // Assert
        var config = await client.GetSsoConfiguration().Success();
        config.Domains.Single().Status.Should().Be(SsoDomainStatus.Verified);
    }

    #endregion

    /// <summary>
    /// The re-check job only picks up domains whose last check is older than the configured window,
    /// and there is no endpoint that moves that clock.
    /// </summary>
    private async Task AgeSsoDomainCheck(string domain)
    {
        await using var ctx = _back.GetDbContext();

        var allowedDomain = await ctx.WebSsoAllowedDomains.Where(d => d.Domain == domain).FirstAsync();
        allowedDomain.LastCheckedAt = DateTime.UtcNow.AddYears(-1);

        await ctx.SaveChangesAsync();
    }
}

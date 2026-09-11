namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.VerifySsoDomain(Guid.NewGuid(), "universidade.edu.br");

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.VerifySsoDomain(Guid.NewGuid(), "universidade.edu.br");

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase("sem-ponto")]
    [TestCase("dominio com espaco.com")]
    [TestCase("https://universidade.edu.br")]
    public async Task Identity_VerifySsoDomain_Should_not_verify_an_invalid_domain(string domain)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain);

        // Assert
        result.ShouldBeError(InvalidSsoDomain.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_sso_configuration_does_not_exist()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        // Act
        var result = await director.VerifySsoDomain(Guid.NewGuid(), director.User.Email.GetEmailDomain());

        // Assert
        result.ShouldBeError(SsoConfigurationNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_of_another_institution_sso_configuration()
    {
        // Arrange
        var owner = await _back.LoggedAsDirector();
        var config = await owner.CreateSsoConfiguration().Success();
        var domain = owner.User.Email.GetEmailDomain();

        var outsider = await _back.LoggedAsDirector();

        // Act
        var result = await outsider.VerifySsoDomain(config.Id, domain);

        // Assert
        result.ShouldBeError(SsoConfigurationNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_that_is_not_in_the_sso_configuration()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();

        // Act
        var result = await director.VerifySsoDomain(config.Id, $"outro-dominio-{DataGen.Numbers}.com");

        // Assert
        result.ShouldBeError(SsoDomainNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_the_txt_record_does_not_exist()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.ShouldBeError(SsoDomainVerificationRecordNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_the_txt_record_has_another_token()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(domain.TxtRecordName, "estud-domain-verification=token-de-outra-instituicao");

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.ShouldBeError(SsoDomainVerificationRecordNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_the_txt_record_is_published_on_the_domain_apex()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(domain.Domain, domain.TxtRecordValue);

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.ShouldBeError(SsoDomainVerificationRecordNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_when_the_dns_lookup_fails()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsServerFailure(domain.TxtRecordName);

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.ShouldBeError(SsoDomainDnsLookupFailed.I);

        var status = (await director.GetSsoConfiguration().Success()).Domains.Single().Status;
        status.Should().Be(SsoDomainStatus.Pending);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_domain_already_verified_by_another_institution()
    {
        // Arrange
        var domain = $"sso-disputado-{DataGen.Numbers}.com";

        var claimant = await _back.LoggedAsDirector($"claimant@{domain}");
        var config = await claimant.CreateSsoConfiguration().Success();
        var claimed = (await claimant.GetSsoConfiguration().Success()).Domains.Single();

        var owner = await _back.LoggedAsDirector($"owner@{domain}");
        await owner.ShortcutCreateVerifiedSsoConfiguration();

        await MocksFactory.PublishDnsTxtRecords(claimed.TxtRecordName, claimed.TxtRecordValue);

        // Act
        var result = await claimant.VerifySsoDomain(config.Id, domain);

        // Assert
        result.ShouldBeError(SsoDomainVerifiedByAnotherInstitution.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_domain_when_the_txt_record_has_the_token()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(domain.TxtRecordName, domain.TxtRecordValue);

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        var verified = result.Success;
        verified.Domain.Should().Be(domain.Domain);
        verified.Status.Should().Be(SsoDomainStatus.Verified);
        verified.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var stored = (await director.GetSsoConfiguration().Success()).Domains.Single();
        stored.Status.Should().Be(SsoDomainStatus.Verified);
        stored.VerifiedAt.Should().BeCloseTo(verified.VerifiedAt!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_domain_when_the_name_has_other_txt_records()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(
            domain.TxtRecordName,
            "v=spf1 include:_spf.google.com ~all",
            domain.TxtRecordValue,
            $"texto-longo-{new string('x', 300)}");

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_domain_informed_in_another_case()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.CreateSsoConfiguration().Success();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(domain.TxtRecordName, domain.TxtRecordValue);

        // Act
        var result = await director.VerifySsoDomain(config.Id, $" {domain.Domain.ToUpperInvariant()} ");

        // Assert
        result.Success.Domain.Should().Be(domain.Domain);
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_keep_domain_verified_when_verifying_it_again()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var config = await director.ShortcutCreateVerifiedSsoConfiguration();
        var domain = (await director.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(domain.TxtRecordName);

        // Act
        var result = await director.VerifySsoDomain(config.Id, domain.Domain);

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
        result.Success.VerifiedAt.Should().Be(domain.VerifiedAt);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_domain_claimed_as_pending_by_another_institution()
    {
        // Arrange
        var domain = $"sso-reivindicado-{DataGen.Numbers}.com";

        var squatter = await _back.LoggedAsDirector($"squatter@{domain}");
        await squatter.CreateSsoConfiguration().Success();

        var owner = await _back.LoggedAsDirector($"owner@{domain}");
        var config = await owner.CreateSsoConfiguration().Success();
        var claimed = (await owner.GetSsoConfiguration().Success()).Domains.Single();

        await MocksFactory.PublishDnsTxtRecords(claimed.TxtRecordName, claimed.TxtRecordValue);

        // Act
        var result = await owner.VerifySsoDomain(config.Id, domain);

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);

        var squatterDomain = (await squatter.GetSsoConfiguration().Success()).Domains.Single();
        squatterDomain.Status.Should().Be(SsoDomainStatus.Pending);
    }

    #endregion
}

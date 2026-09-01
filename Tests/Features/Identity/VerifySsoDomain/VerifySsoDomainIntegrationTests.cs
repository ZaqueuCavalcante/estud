using Estud.Back.Domain.Identity;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.VerifySsoDomain("sso-verify-unauthenticated.com");

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.VerifySsoDomain("sso-verify-forbidden.com");

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase("nao-configurado.com")]
    [TestCase("nao-e-dominio")]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_that_is_not_configured(string domain)
    {
        // Arrange
        var client = await _back.LoggedAsDirector($"director@{DataGen.Numbers}-sso-verify-not-found.com");
        await client.CreateSsoConfiguration().Success();

        // Act
        var result = await client.VerifySsoDomain(domain);

        // Assert
        result.ShouldBeError(SsoDomainNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_of_another_institution()
    {
        // Arrange
        var owner = await _back.LoggedAsDirector("director@sso-verify-owner.com");
        var created = await owner.CreateSsoConfiguration().Success();
        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);

        var other = await _back.LoggedAsDirector("director@sso-verify-intruder.com");

        // Act
        var result = await other.VerifySsoDomain("sso-verify-owner.com");

        // Assert
        result.ShouldBeError(SsoDomainNotFound.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_when_txt_record_is_missing()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-no-record.com");
        await client.CreateSsoConfiguration().Success();

        // Act
        var result = await client.VerifySsoDomain("sso-verify-no-record.com");

        // Assert
        result.ShouldBeError(SsoDomainVerificationFailed.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_not_verify_sso_domain_when_txt_record_has_another_token()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-wrong-token.com");
        var created = await client.CreateSsoConfiguration().Success();

        await _mocks.SetDnsTxtRecord(
            created.RecordName,
            $"{SsoAllowedDomain.RecordValuePrefix}00000000000000000000000000000000");

        // Act
        var result = await client.VerifySsoDomain("sso-verify-wrong-token.com");

        // Assert
        result.ShouldBeError(SsoDomainVerificationFailed.I);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_mark_domain_as_failed_when_verification_does_not_pass()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-failed-status.com");
        await client.CreateSsoConfiguration().Success();

        // Act
        await client.VerifySsoDomain("sso-verify-failed-status.com");

        // Assert
        var config = await client.GetSsoConfiguration().Success();
        var domain = config.Domains.Single();
        domain.Status.Should().Be(SsoDomainStatus.Failed);
        domain.VerifiedAt.Should().BeNull();
        domain.LastCheckedAt.Should().NotBeNull();
        domain.LastError.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_sso_domain_when_txt_record_matches()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-happy-path.com");
        var created = await client.CreateSsoConfiguration().Success();

        created.Domain.Should().Be("sso-verify-happy-path.com");
        created.DomainStatus.Should().Be(SsoDomainStatus.Pending);
        created.RecordName.Should().Be($"{SsoAllowedDomain.RecordPrefix}.sso-verify-happy-path.com");

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);

        // Act
        var result = await client.VerifySsoDomain(created.Domain);

        // Assert
        var verified = result.Success;
        verified.Domain.Should().Be("sso-verify-happy-path.com");
        verified.Status.Should().Be(SsoDomainStatus.Verified);
        verified.VerifiedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_sso_domain_when_txt_record_is_among_other_records()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-many-records.com");
        var created = await client.CreateSsoConfiguration().Success();

        await _mocks.SetDnsTxtRecord(
            created.RecordName,
            "v=spf1 include:_spf.google.com ~all",
            created.RecordValue,
            "google-site-verification=abcdef");

        // Act
        var result = await client.VerifySsoDomain(created.Domain);

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_sso_domain_ignoring_case_and_trailing_dot()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-case.com");
        var created = await client.CreateSsoConfiguration().Success();
        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);

        // Act
        var result = await client.VerifySsoDomain("SSO-VERIFY-CASE.COM");

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_verify_sso_domain_again_when_already_verified()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-twice.com");
        var created = await client.CreateSsoConfiguration().Success();
        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await client.VerifySsoDomain(created.Domain).Success();

        // Act
        var result = await client.VerifySsoDomain(created.Domain);

        // Assert
        result.Success.Status.Should().Be(SsoDomainStatus.Verified);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_expire_domain_when_txt_record_is_removed_after_verification()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-verify-expired.com");
        var created = await client.CreateSsoConfiguration().Success();
        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await client.VerifySsoDomain(created.Domain).Success();

        await _mocks.SetDnsTxtRecord(created.RecordName, "v=spf1 -all");

        // Act
        var result = await client.VerifySsoDomain(created.Domain);

        // Assert
        result.ShouldBeError(SsoDomainVerificationFailed.I);

        var config = await client.GetSsoConfiguration().Success();
        config.Domains.Single().Status.Should().Be(SsoDomainStatus.Expired);
    }

    [Test]
    public async Task Identity_VerifySsoDomain_Should_enable_sso_only_after_the_domain_is_verified()
    {
        // Arrange
        var director = await _back.LoggedAsDirector("director@sso-verify-enables-login.com");
        var created = await director.CreateSsoConfiguration().Success();

        var anonymous = _back.GetTestsClient();

        var before = await anonymous.CheckSsoAvailability("someone@sso-verify-enables-login.com").Success();
        before.SsoEnabled.Should().BeFalse();

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);

        // Act
        await director.VerifySsoDomain(created.Domain).Success();

        // Assert
        var after = await anonymous.CheckSsoAvailability("someone@sso-verify-enables-login.com").Success();
        after.SsoEnabled.Should().BeTrue();
    }

    #endregion
}

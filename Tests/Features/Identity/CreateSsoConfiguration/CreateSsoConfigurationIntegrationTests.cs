using Estud.Back.Features.Identity.EmailPasswordLogin;
using Estud.Back.Features.Identity.CreateSsoConfiguration;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase((SsoProviderType)99)]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_invalid_provider_type(SsoProviderType providerType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(providerType: providerType);

        // Assert
        result.ShouldBeError(InvalidSsoProviderType.I);
    }

    [Test]
    [TestCase("")]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_invalid_authority(string authority)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(authority: authority);

        // Assert
        result.ShouldBeError(InvalidSsoAuthority.I);
    }

    [Test]
    [TestCase("")]
    [TestCase("abc")]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_invalid_client_id(string clientId)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(clientId: clientId);

        // Assert
        result.ShouldBeError(InvalidSsoClientId.I);
    }

    [Test]
    [TestCase("")]
    [TestCase("short")]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_invalid_client_secret(string clientSecret)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(clientSecret: clientSecret);

        // Assert
        result.ShouldBeError(InvalidSsoClientSecret.I);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_authority_is_not_https()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(authority: "http://login.microsoftonline.com/tenant-id/v2.0");

        // Assert
        result.ShouldBeError(SsoAuthorityMustBeHttps.I);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_authority_has_user_info()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(authority: "https://evil.com@login.microsoftonline.com/tenant-id/v2.0");

        // Assert
        result.ShouldBeError(SsoAuthorityHasUserInfo.I);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_authority_is_link_local()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(authority: "https://169.254.169.254/v2.0");

        // Assert
        result.ShouldBeError(SsoAuthorityLinkLocalNotAllowed.I);
    }

    [Test]
    [TestCase("")]
    [TestCase("sem-ponto")]
    [TestCase("escola .edu.br")]
    [TestCase("https://escola.edu.br")]
    [TestCase("professor@escola.edu.br")]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_invalid_domain(string domain)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(domain: domain);

        // Assert
        result.ShouldBeError(InvalidSsoDomain.I);
    }

    [Test]
    [TestCase("gmail.com")]
    [TestCase("outlook.com")]
    [TestCase("hotmail.com")]
    [TestCase("yahoo.com.br")]
    [TestCase("icloud.com")]
    [TestCase("proton.me")]
    [TestCase("uol.com.br")]
    [TestCase("mailinator.com")]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_with_public_email_domain(string domain)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(domain: domain);

        // Assert
        result.ShouldBeError(SsoPublicDomainNotAllowed.I);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_domain_already_configured()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-duplicate-domain.com");
        await client.CreateSsoConfiguration();

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        result.ShouldBeError(SsoDomainAlreadyConfigured.I);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_create_sso_configuration_when_domain_is_verified_by_another_institution()
    {
        // Arrange
        var domain = $"sso-verificado-{DataGen.Numbers}.com";

        var owner = await _back.LoggedAsDirector();
        await owner.ShortcutCreateVerifiedSsoConfiguration(domain: domain);

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(domain: domain);

        // Assert
        result.ShouldBeError(SsoDomainAlreadyConfigured.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration()
    {
        // Arrange
        var client = await _back.LoggedAsDirector("director@sso-happy-path.com");

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        var config = result.Success;
        config.Id.Should().NotBeEmpty();
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration_for_a_domain_other_than_the_director_email_domain()
    {
        // Arrange
        var domain = $"escola-{DataGen.Numbers}.edu.br";
        var client = await _back.LoggedAsDirector($"{DataGen.Numbers}.diretor@gmail.com");

        // Act
        var result = await client.CreateSsoConfiguration(domain: domain);

        // Assert
        result.Success.Id.Should().NotBeEmpty();

        var created = (await client.GetSsoConfiguration().Success()).Domains.Single();
        created.Domain.Should().Be(domain);
        created.Status.Should().Be(SsoDomainStatus.Pending);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration_with_the_domain_normalized()
    {
        // Arrange
        var domain = $"escola-{DataGen.Numbers}.edu.br";
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateSsoConfiguration(domain: $"  @{domain.ToUpperInvariant()} ");

        // Assert
        result.Success.Id.Should().NotBeEmpty();

        var created = (await client.GetSsoConfiguration().Success()).Domains.Single();
        created.Domain.Should().Be(domain);
        created.TxtRecordName.Should().Be($"_estud-challenge.{domain}");
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration_with_the_domain_pending_verification()
    {
        // Arrange
        var domain = $"sso-create-pending-{DataGen.Numbers}.com";
        var client = await _back.LoggedAsDirector($"director@{domain}");

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        result.Success.Id.Should().NotBeEmpty();

        var created = (await client.GetSsoConfiguration().Success()).Domains.Single();
        created.Domain.Should().Be(domain);
        created.Status.Should().Be(SsoDomainStatus.Pending);
        created.VerifiedAt.Should().BeNull();
        created.TxtRecordName.Should().Be($"_estud-challenge.{domain}");
        created.TxtRecordValue.Should().MatchRegex("^estud-domain-verification=[0-9a-f]{32}$");

        var availability = await client.CheckSsoAvailability($"usuario@{domain}").Success();
        availability.SsoEnabled.Should().BeFalse();
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration_when_domain_is_pending_in_another_institution()
    {
        // Arrange
        var domain = $"sso-pendente-{DataGen.Numbers}.com";

        var squatter = await _back.LoggedAsDirector($"squatter@{domain}");
        await squatter.CreateSsoConfiguration().Success();

        var client = await _back.LoggedAsDirector($"director@{domain}");

        // Act
        var result = await client.CreateSsoConfiguration();

        // Assert
        result.Success.Id.Should().NotBeEmpty();

        var squatterToken = (await squatter.GetSsoConfiguration().Success()).Domains.Single().TxtRecordValue;
        var token = (await client.GetSsoConfiguration().Success()).Domains.Single().TxtRecordValue;
        token.Should().NotBe(squatterToken);
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_create_sso_configuration_requiring_sso()
    {
        // Arrange
        var domain = $"sso-create-required-{DataGen.Numbers}.com";
        var client = await _back.LoggedAsDirector($"director@{domain}");

        // Act
        var result = await client.CreateSsoConfiguration(requireSso: true);

        // Assert
        result.Success.Id.Should().NotBeEmpty();

        var config = await client.GetSsoConfiguration().Success();
        config.RequireSso.Should().BeTrue();

        await client.ShortcutVerifySsoDomain(config.Id);

        var availability = await client.CheckSsoAvailability($"usuario@{domain}").Success();
        availability.SsoRequired.Should().BeTrue();
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_not_block_password_login_while_the_domain_is_pending()
    {
        // Arrange
        var client = await _back.LoggedAsDirector($"director@sso-create-required-{DataGen.Numbers}.com");
        await client.CreateSsoConfiguration(requireSso: true).Success();
        await client.Logout();

        // Act
        var result = await client.EmailPasswordLogin(client.User.Email, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeSuccess();
    }

    [Test]
    public async Task Identity_CreateSsoConfiguration_Should_block_password_login_when_created_requiring_sso()
    {
        // Arrange
        var client = await _back.LoggedAsDirector($"director@sso-create-required-{DataGen.Numbers}.com");
        await client.ShortcutCreateVerifiedSsoConfiguration(requireSso: true);
        await client.Logout();

        // Act
        var result = await client.EmailPasswordLogin(client.User.Email, "My@nEw@strong@P4ssword");

        // Assert
        result.ShouldBeError(SsoLoginRequired.I);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("nao-e-email")]
    [TestCase("sem-arroba.com")]
    public async Task Identity_SsoChallenge_Should_not_challenge_with_an_invalid_email(string? email)
    {
        // Arrange
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SsoChallenge(email);

        // Assert
        challenge.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(InvalidEmail)}");
    }

    [Test]
    public async Task Identity_SsoChallenge_Should_not_challenge_when_domain_has_no_sso_configured()
    {
        // Arrange
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SsoChallenge($"alguem@sem-sso-{DataGen.Numbers}.com");

        // Assert
        challenge.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoNotConfiguredForDomain)}");
    }

    [Test]
    public async Task Identity_SsoChallenge_Should_not_challenge_when_sso_configuration_is_inactive()
    {
        // Arrange
        var domain = $"sso-inativo-{DataGen.Numbers}.com";
        var director = await _back.LoggedAsDirector($"director@{domain}");

        var config = await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        await director.UpdateSsoConfiguration(config.Id, authority: MocksFactory.OidcAuthority, isActive: false);

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SsoChallenge($"director@{domain}");

        // Assert
        challenge.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoNotConfiguredForDomain)}");
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_SsoChallenge_Should_redirect_to_the_identity_provider_authorization_endpoint()
    {
        // Arrange
        var domain = $"sso-challenge-{DataGen.Numbers}.com";
        var email = $"director@{domain}";
        var director = await _back.LoggedAsDirector(email);

        var config = await director.CreateSsoConfiguration(
            authority: MocksFactory.OidcAuthority,
            clientId: "estud-oidc-client").Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SsoChallenge(email);

        // Assert
        var location = challenge.Headers.Location?.ToString();
        location.Should().StartWith($"{MocksFactory.OidcAuthority}/connect/authorize?");
        location.Should().Contain("response_type=code");
        location.Should().Contain("client_id=estud-oidc-client");
        location.Should().Contain($"identity%2Fsso%2Fcallback%2F{config.Id}");
        location.Should().Contain("openid");
        location.Should().Contain("login_hint=");
        location.Should().Contain("code_challenge=");
        location.Should().Contain("state=");
        location.Should().Contain("nonce=");
    }

    #endregion
}

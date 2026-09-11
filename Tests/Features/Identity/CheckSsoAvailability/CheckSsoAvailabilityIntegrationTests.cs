namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase("not-an-email")]
    [TestCase("usuario@")]
    public async Task Identity_CheckSsoAvailability_Should_not_check_sso_availability_with_invalid_email(string email)
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CheckSsoAvailability(email);

        // Assert
        result.ShouldBeError(InvalidEmail.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_CheckSsoAvailability_Should_return_sso_not_enabled_when_domain_has_no_configuration()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CheckSsoAvailability("usuario@sem-sso-configurado.com");

        // Assert
        var availability = result.Success;
        availability.SsoEnabled.Should().BeFalse();
        availability.SsoRequired.Should().BeFalse();
        availability.ProviderType.Should().BeNull();
    }

    [Test]
    public async Task Identity_CheckSsoAvailability_Should_return_sso_not_enabled_while_domain_is_pending_verification()
    {
        // Arrange
        var domain = $"sso-check-pending-{DataGen.Numbers}.com";
        var director = await _back.LoggedAsDirector($"director@{domain}");
        await director.CreateSsoConfiguration(requireSso: true).Success();

        var client = _back.GetTestsClient();

        // Act
        var result = await client.CheckSsoAvailability($"someone@{domain}");

        // Assert
        var availability = result.Success;
        availability.SsoEnabled.Should().BeFalse();
        availability.SsoRequired.Should().BeFalse();
        availability.ProviderType.Should().BeNull();
    }

    [Test]
    public async Task Identity_CheckSsoAvailability_Should_return_sso_enabled_when_domain_has_configuration()
    {
        // Arrange
        var director = await _back.LoggedAsDirector("director@sso-check-available.com");
        await director.ShortcutCreateVerifiedSsoConfiguration(providerType: SsoProviderType.AzureAd);

        var client = _back.GetTestsClient();

        // Act
        var result = await client.CheckSsoAvailability("someone@sso-check-available.com");

        // Assert
        var availability = result.Success;
        availability.SsoEnabled.Should().BeTrue();
        availability.SsoRequired.Should().BeFalse();
        availability.ProviderType.Should().Be(SsoProviderType.AzureAd);
    }

    #endregion
}

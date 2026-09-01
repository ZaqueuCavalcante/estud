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
    public async Task Identity_CheckSsoAvailability_Should_return_sso_not_enabled_when_domain_is_not_verified()
    {
        // Arrange
        var director = await _back.LoggedAsDirector("director@sso-check-unverified.com");
        await director.CreateSsoConfiguration(providerType: SsoProviderType.AzureAd).Success();

        var client = _back.GetTestsClient();

        // Act
        var result = await client.CheckSsoAvailability("someone@sso-check-unverified.com");

        // Assert
        var availability = result.Success;
        availability.SsoEnabled.Should().BeFalse();
        availability.ProviderType.Should().BeNull();
    }

    [Test]
    public async Task Identity_CheckSsoAvailability_Should_return_sso_enabled_when_domain_has_configuration()
    {
        // Arrange
        var director = await _back.LoggedAsDirector("director@sso-check-available.com");
        var created = await director.CreateSsoConfiguration(providerType: SsoProviderType.AzureAd).Success();

        await _mocks.SetDnsTxtRecord(created.RecordName, created.RecordValue);
        await director.VerifySsoDomain(created.Domain).Success();

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

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_login_with_google_and_auto_provision_new_user()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be("http://localhost:3000/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(email);
    }

    #endregion
}

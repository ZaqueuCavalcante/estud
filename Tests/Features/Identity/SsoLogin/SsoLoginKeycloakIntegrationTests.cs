namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Identity_SsoLogin_Should_prefill_the_keycloak_login_with_the_informed_email()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.ShortcutCreateVerifiedSsoConfiguration(
            SsoProviderType.CustomOidc,
            KeycloakFactory.Authority,
            KeycloakFactory.ClientId,
            KeycloakFactory.ClientSecret);

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var page = await client.SsoOpenKeycloakLoginPage(director.User.Email);

        // Assert
        page.Username.Should().Be(director.User.Email);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_login_through_keycloak()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.ShortcutCreateVerifiedSsoConfiguration(
            SsoProviderType.CustomOidc,
            KeycloakFactory.Authority,
            KeycloakFactory.ClientId,
            KeycloakFactory.ClientSecret);

        var email = director.User.Email;
        await KeycloakFactory.CreateUser(email);

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLoginWithKeycloak(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(email);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Identity_SsoLogin_Should_reflect_the_keycloak_email_verified_on_the_email_confirmation(bool emailVerified)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.ShortcutCreateVerifiedSsoConfiguration(
            SsoProviderType.CustomOidc,
            KeycloakFactory.Authority,
            KeycloakFactory.ClientId,
            KeycloakFactory.ClientSecret);
        var domain = director.User.Email.GetEmailDomain();

        var teacherEmail = $"professor.{DataGen.Numbers}@{domain}";
        await director.CreateTeacher(DataGen.UserName, teacherEmail).Success();
        await KeycloakFactory.CreateUser(teacherEmail, emailVerified);

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLoginWithKeycloak(teacherEmail);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        var userId = account.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().Be(emailVerified);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    [TestCase("42")]
    [TestCase("apple")]
    [TestCase("Facebook")]
    public async Task Identity_SocialLoginChallenge_Should_not_challenge_unsupported_provider(string provider)
    {
        // Arrange
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SocialLoginChallenge(provider);

        // Assert
        challenge.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginFailed)}");
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_when_email_is_not_verified()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, emailVerified: false);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginEmailNotVerified)}");

        await using var ctx = _back.GetDbContext();
        var created = await ctx.Users.AnyAsync(u => u.Email == email);
        created.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_when_email_domain_requires_sso()
    {
        // Arrange
        var domain = $"sso-required-{DataGen.Numbers}.com";
        var director = await _back.LoggedAsDirector($"director@{domain}");

        var config = await director.CreateSsoConfiguration().Success();
        await director.UpdateSsoConfiguration(config.Id, requireSso: true);

        var email = $"someone@{domain}";
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginSsoRequired)}");

        await using var ctx = _back.GetDbContext();
        var created = await ctx.Users.AnyAsync(u => u.Email == email);
        created.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_when_google_does_not_return_the_email()
    {
        // Arrange
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin("");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_when_google_returns_an_error()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, providerError: "access_denied");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginFailed)}");

        await using var ctx = _back.GetDbContext();
        var created = await ctx.Users.AnyAsync(u => u.Email == email);
        created.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_when_authorization_code_is_invalid()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, invalidCode: true);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginFailed)}");

        await using var ctx = _back.GetDbContext();
        var created = await ctx.Users.AnyAsync(u => u.Email == email);
        created.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_login_without_the_correlation_cookie()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, withCorrelationCookie: false);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginFailed)}");

        await using var ctx = _back.GetDbContext();
        var created = await ctx.Users.AnyAsync(u => u.Email == email);
        created.Should().BeFalse();
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_redirect_to_the_google_authorization_endpoint()
    {
        // Arrange
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var challenge = await client.SocialLoginChallenge("google");

        // Assert
        var location = challenge.Headers.Location?.ToString();
        location.Should().StartWith($"{MocksFactory.Url}/social-login/google/authorize?");
        location.Should().Contain("response_type=code");
        location.Should().Contain("client_id=test-google-client-id");
        location.Should().Contain("identity%2Fsocial-login%2Fcallback%2Fgoogle");
        location.Should().Contain("openid");
        location.Should().Contain("state=");
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_login_with_google_and_auto_provision_new_user()
    {
        // Arrange
        var email = DataGen.Email;
        var subject = Guid.NewGuid().ToString();
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, subject: subject, givenName: "Ada", familyName: "Lovelace");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(email);
        account.Name.Should().Be("Ada Lovelace");
        account.InstitutionId.Should().BeGreaterThan(0);

        var userId = account.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().BeTrue();

        var hasLink = await ctx.UserSocialLogins.AnyAsync(x =>
            x.UserId == userId && x.Provider == SocialLoginProvider.Google && x.ProviderKey == subject);
        hasLink.Should().BeTrue();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_login_returning_user_with_existing_link()
    {
        // Arrange
        var email = DataGen.Email;
        var subject = Guid.NewGuid().ToString();

        var firstClient = _back.GetTestsClient(followRedirects: false);
        await firstClient.GoogleSocialLogin(email, subject: subject);
        var first = await firstClient.GetUserAccount().Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act — same Google account (same subject) logging in again
        var callback = await client.GoogleSocialLogin(email, subject: subject);

        // Assert — same user and institution reused, single social login link
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var second = await client.GetUserAccount().Success();
        second.Id.Should().Be(first.Id);
        second.InstitutionId.Should().Be(first.InstitutionId);

        var userId = first.Id;

        await using var ctx = _back.GetDbContext();
        var links = await ctx.UserSocialLogins.CountAsync(x => x.UserId == userId);
        links.Should().Be(1);
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_link_and_login_existing_user_by_email()
    {
        // Arrange — user already exists (registered, e-mail not yet confirmed)
        var email = DataGen.Email;
        var registerClient = _back.GetTestsClient();
        var existing = await registerClient.RegisterUser(email).Success();

        var subject = Guid.NewGuid().ToString();
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, subject: subject);

        // Assert — same user and institution reused, account linked and e-mail confirmed
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Id.Should().Be(existing.Id);
        account.InstitutionId.Should().Be(existing.InstitutionId);

        var userId = existing.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().BeTrue();

        var hasLink = await ctx.UserSocialLogins.AnyAsync(x =>
            x.UserId == userId && x.Provider == SocialLoginProvider.Google && x.ProviderKey == subject);
        hasLink.Should().BeTrue();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_use_the_name_claim_when_google_has_no_given_name()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, name: "Grace Hopper");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Name.Should().Be("Grace Hopper");
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_use_email_as_name_when_google_returns_no_name()
    {
        // Arrange
        var email = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Name.Should().Be(email);
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_match_existing_user_ignoring_email_case()
    {
        // Arrange — existing user stored in lowercase
        var email = DataGen.Email;
        var registerClient = _back.GetTestsClient();
        var existing = await registerClient.RegisterUser(email).Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act — Google returns the same e-mail in a different case
        var callback = await client.GoogleSocialLogin(email.ToUpperInvariant());

        // Assert — matched the existing user and its institution, no duplicate
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Id.Should().Be(existing.Id);
        account.InstitutionId.Should().Be(existing.InstitutionId);
    }

    #endregion

    #region Security

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_not_link_existing_user_when_email_is_not_verified()
    {
        // Arrange — existing unconfirmed user
        var email = DataGen.Email;
        var registerClient = _back.GetTestsClient();
        var existing = await registerClient.RegisterUser(email).Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.GoogleSocialLogin(email, emailVerified: false);

        // Assert — rejected, no side effects (no link, e-mail still not confirmed)
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/?social_login_error={nameof(SocialLoginEmailNotVerified)}");

        var userId = existing.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().BeFalse();

        var hasLink = await ctx.UserSocialLogins.AnyAsync(x => x.UserId == userId);
        hasLink.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SocialLoginChallenge_Should_login_by_provider_key_even_when_email_changed()
    {
        // Arrange — account first provisioned with email A and subject S
        var emailA = DataGen.Email;
        var subject = Guid.NewGuid().ToString();

        var firstClient = _back.GetTestsClient(followRedirects: false);
        await firstClient.GoogleSocialLogin(emailA, subject: subject);
        var first = await firstClient.GetUserAccount().Success();

        var emailB = DataGen.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act — same Google account (subject S) but now reporting a different e-mail
        var callback = await client.GoogleSocialLogin(emailB, subject: subject);

        // Assert — identity is anchored to the provider key, not the e-mail
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Id.Should().Be(first.Id);
        account.Email.Should().Be(emailA);

        await using var ctx = _back.GetDbContext();
        var createdForEmailB = await ctx.Users.AnyAsync(u => u.Email == emailB);
        createdForEmailB.Should().BeFalse();
    }

    #endregion
}

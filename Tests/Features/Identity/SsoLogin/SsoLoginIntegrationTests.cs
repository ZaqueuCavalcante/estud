namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_identity_provider_does_not_return_the_email()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, noEmail: true);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoAuthenticationFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_identity_provider_returns_an_error()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, providerError: "access_denied");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoAuthenticationFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_authorization_code_is_invalid()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, invalidCode: true);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoAuthenticationFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_without_the_correlation_cookie()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, withCorrelationCookie: false);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoAuthenticationFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_user_does_not_exist_in_the_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var domain = director.User.Email.GetEmailDomain();
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin($"nao-existe@{domain}");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoLoginUserNotFound)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Identity_SsoLogin_Should_login_the_user_who_configured_the_sso()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(email);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_login_another_user_of_the_same_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var domain = director.User.Email.GetEmailDomain();

        var teacherEmail = $"professor.{DataGen.Numbers}@{domain}";
        await director.CreateTeacher(DataGen.UserName, teacherEmail).Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(teacherEmail);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(teacherEmail);
        account.InstitutionId.Should().Be(director.User.InstitutionId);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_login_the_same_user_on_a_second_login()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;

        var firstClient = _back.GetTestsClient(followRedirects: false);
        await firstClient.SsoLogin(email);
        var first = await firstClient.GetUserAccount().Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var second = await client.GetUserAccount().Success();
        second.Id.Should().Be(first.Id);
        second.InstitutionId.Should().Be(first.InstitutionId);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_confirm_the_email_when_the_identity_provider_verified_it()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var domain = director.User.Email.GetEmailDomain();

        var teacherEmail = $"professor.{DataGen.Numbers}@{domain}";
        await director.CreateTeacher(DataGen.UserName, teacherEmail).Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(teacherEmail, emailVerified: true);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        var userId = account.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_confirm_the_email_when_the_identity_provider_did_not_verify_it()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var domain = director.User.Email.GetEmailDomain();

        var teacherEmail = $"professor.{DataGen.Numbers}@{domain}";
        await director.CreateTeacher(DataGen.UserName, teacherEmail).Success();

        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(teacherEmail, emailVerified: false);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        var userId = account.Id;

        await using var ctx = _back.GetDbContext();
        var user = await ctx.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        user.EmailConfirmed.Should().BeFalse();
    }

    [Test]
    public async Task Identity_SsoLogin_Should_login_when_the_identity_provider_returns_the_email_in_another_case()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, idpEmail: email.ToUpperInvariant());

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/home");

        var account = await client.GetUserAccount().Success();
        account.Email.Should().Be(email);
    }

    #endregion

    #region Security

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_identity_provider_returns_an_email_from_another_domain()
    {
        // Arrange — o IdP autentica, mas devolve um e-mail fora dos domínios da configuração
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, idpEmail: $"invasor@outro-dominio-{DataGen.Numbers}.com");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoNotConfiguredForDomain)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_when_the_userinfo_subject_does_not_match_the_id_token()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;
        var client = _back.GetTestsClient(followRedirects: false);

        // Act
        var callback = await client.SsoLogin(email, subject: "sub-do-id-token", userInfoSubject: "outro-sub");

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoAuthenticationFailed)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Identity_SsoLogin_Should_not_login_a_user_of_another_institution_through_this_configuration()
    {
        // Arrange — outra instituição, com o seu próprio domínio e sem SSO configurado
        var director = await _back.LoggedAsDirector();
        await director.CreateSsoConfiguration(authority: MocksFactory.OidcAuthority).Success();
        var email = director.User.Email;

        var outsiderEmail = $"de-fora.{DataGen.Numbers}@instituicao-vizinha-{DataGen.Numbers}.com";
        await _back.LoggedAsDirector(outsiderEmail);

        var client = _back.GetTestsClient(followRedirects: false);

        // Act — o challenge sai pelo scheme do domínio configurado, o IdP devolve o e-mail de fora
        var callback = await client.SsoLogin(email, idpEmail: outsiderEmail);

        // Assert
        callback.Headers.Location?.ToString().Should().Be($"{FrontUrl}/login?sso_error={nameof(SsoNotConfiguredForDomain)}");

        var status = await client.GetAuthStatus();
        status.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion
}

using System.Net.Http.Json;
using Estud.Back.Features.Identity.GetRole;
using Estud.Back.Features.Identity.GetRoles;
using Estud.Back.Features.Identity.CreateRole;
using Estud.Back.Features.Identity.UpdateRole;
using Estud.Back.Features.Identity.ResetPassword;
using Estud.Back.Features.Identity.GetPermissions;
using Estud.Back.Features.Identity.MagicLinkLogin;
using Estud.Back.Features.Identity.SetupTwoFactor;
using Estud.Back.Features.Identity.TwoFactorLogin;
using Estud.Back.Features.Identity.GetTwoFactorKey;
using Estud.Back.Features.Identity.GoogleOneTapLogin;
using Estud.Back.Features.Identity.EmailPasswordLogin;
using Estud.Back.Features.Identity.GetSsoConfiguration;
using Estud.Back.Features.Identity.TwoFactorSetupLogin;
using Estud.Back.Features.Identity.CheckSsoAvailability;
using Estud.Back.Features.Identity.CreateSsoConfiguration;
using Estud.Back.Features.Identity.UpdateSsoConfiguration;
using Estud.Back.Features.Identity.SendResetPasswordToken;
using Estud.Back.Features.Identity.SetTwoFactorEnforcement;
using Estud.Back.Features.Identity.GetTwoFactorEnforcement;
using Estud.Back.Features.Identity.CheckSocialLoginAvailability;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<MagicLinkLoginOut, ErrorOut>> MagicLinkLogin(string? token)
    {
        var data = new MagicLinkLoginIn { Token = token };

        var response = await http.PostAsJsonAsync("identity/magic-link-login", data);

        return await response.Resolve<MagicLinkLoginOut>();
    }

    public async Task<OneOf<EmailPasswordLoginOut, ErrorOut>> EmailPasswordLogin(string email, string password)
    {
        var data = new EmailPasswordLoginIn { Email = email, Password = password };

        var response = await http.PostAsJsonAsync("identity/email-password-login", data);

        return await response.Resolve<EmailPasswordLoginOut>();
    }

    public async Task<HttpResponseMessage> Logout()
    {
        return await http.PostAsJsonAsync("identity/logout", new {});
    }

    public async Task<OneOf<GetTwoFactorKeyOut, ErrorOut>> GetTwoFactorKey()
    {
        var response = await http.GetAsync("identity/2fa-key");
        return await response.Resolve<GetTwoFactorKeyOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> SetupTwoFactor(string token)
    {
        var data = new SetupTwoFactorIn { Token = token };
        var response = await http.PostAsJsonAsync("identity/2fa-setup", data);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<TwoFactorLoginOut, ErrorOut>> TwoFactorLogin(string? token)
    {
        var body = new TwoFactorLoginIn { Token = token };

        var response = await http.PostAsJsonAsync("identity/2fa-login", body);

        return await response.Resolve<TwoFactorLoginOut>();
    }

    public async Task<OneOf<TwoFactorSetupLoginOut, ErrorOut>> TwoFactorSetupLogin()
    {
        var response = await http.PostAsJsonAsync("identity/2fa-setup-login", new {});

        return await response.Resolve<TwoFactorSetupLoginOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> SendResetPasswordToken(string email)
    {
        var data = new SendResetPasswordTokenIn { Email = email };
        var response = await http.PostAsJsonAsync("identity/reset-password-token", data);

        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> ResetPassword(string token, string password)
    {
        var data = new ResetPasswordIn { Token = token, Password = password };
        var response = await http.PostAsJsonAsync("identity/reset-password", data);

        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<CreateRoleOut, ErrorOut>> CreateRole(
        string name = "Admin",
        string description = "Administrador com acesso total",
        UserType baseType = UserType.Manager,
        List<int>? permissions = null
    ) {
        var data = new CreateRoleIn { Name = name, Description = description, BaseType = baseType, Permissions = permissions ?? [] };
        var response = await http.PostAsJsonAsync("identity/roles", data);
        return await response.Resolve<CreateRoleOut>();
    }

    public async Task<OneOf<GetRolesOut, ErrorOut>> GetRoles()
    {
        var response = await http.GetAsync("identity/roles");
        return await response.Resolve<GetRolesOut>();
    }

    public async Task<OneOf<GetRoleOut, ErrorOut>> GetRole(int roleId)
    {
        var response = await http.GetAsync($"identity/roles/{roleId}");
        return await response.Resolve<GetRoleOut>();
    }

    public async Task<OneOf<UpdateRoleOut, ErrorOut>> UpdateRole(
        int roleId,
        string name = "Admin",
        string description = "Administrador com acesso total",
        List<int>? permissions = null
    ) {
        var data = new UpdateRoleIn { Name = name, Description = description, Permissions = permissions ?? [] };
        var response = await http.PutAsJsonAsync($"identity/roles/{roleId}", data);
        return await response.Resolve<UpdateRoleOut>();
    }

    public async Task<OneOf<GetPermissionsOut, ErrorOut>> GetPermissions()
    {
        var response = await http.GetAsync("identity/permissions");
        return await response.Resolve<GetPermissionsOut>();
    }

    public async Task<OneOf<GetTwoFactorEnforcementOut, ErrorOut>> GetTwoFactorEnforcement()
    {
        var response = await http.GetAsync("identity/2fa-enforcement");
        return await response.Resolve<GetTwoFactorEnforcementOut>();
    }

    public async Task<OneOf<SetTwoFactorEnforcementOut, ErrorOut>> SetTwoFactorEnforcement(int roleId, bool required)
    {
        var data = new SetTwoFactorEnforcementIn { RoleId = roleId, Required = required };
        var response = await http.PutAsJsonAsync("identity/2fa-enforcement", data);
        return await response.Resolve<SetTwoFactorEnforcementOut>();
    }

    public async Task<OneOf<CreateSsoConfigurationOut, ErrorOut>> CreateSsoConfiguration(
        SsoProviderType providerType = SsoProviderType.AzureAd,
        string authority = "https://login.microsoftonline.com/tenant-id/v2.0",
        string clientId = "00000000-0000-0000-0000-000000000000",
        string clientSecret = "client-secret-value"
    ) {
        var data = new CreateSsoConfigurationIn
        {
            ProviderType = providerType,
            Authority = authority,
            ClientId = clientId,
            ClientSecret = clientSecret,
        };
        var response = await http.PostAsJsonAsync("identity/sso/configurations", data);
        return await response.Resolve<CreateSsoConfigurationOut>();
    }

    public async Task<HttpResponseMessage> SsoChallenge(string? email)
    {
        return await http.GetAsync($"identity/sso/challenge?email={Uri.EscapeDataString(email ?? "")}");
    }

    public async Task<OneOf<CheckSocialLoginAvailabilityOut, ErrorOut>> CheckSocialLoginAvailability()
    {
        var response = await http.GetAsync("identity/social-login/check-availability");
        return await response.Resolve<CheckSocialLoginAvailabilityOut>();
    }

    public async Task<OneOf<GoogleOneTapLoginOut, ErrorOut>> GoogleOneTapLogin(string? credential)
    {
        var data = new GoogleOneTapLoginIn { Credential = credential };
        var response = await http.PostAsJsonAsync("identity/social-login/google-one-tap", data);
        return await response.Resolve<GoogleOneTapLoginOut>();
    }

    public async Task<HttpResponseMessage> SocialLoginChallenge(string provider)
    {
        return await http.GetAsync($"identity/social-login/challenge/{provider}");
    }

    public async Task<HttpResponseMessage> GoogleSocialLogin(
        string email,
        string? subject = null,
        bool emailVerified = true,
        string? name = null,
        string? givenName = null,
        string? familyName = null,
        string? providerError = null,
        bool invalidCode = false,
        bool withCorrelationCookie = true
    ) {
        var challenge = await SocialLoginChallenge("Google");

        var mockParams = new Dictionary<string, string?>
        {
            ["login_hint"] = email,
            ["mock_subject"] = subject,
            ["mock_email_verified"] = emailVerified ? "true" : "false",
            ["mock_name"] = name,
            ["mock_given_name"] = givenName,
            ["mock_family_name"] = familyName,
            ["mock_error"] = providerError,
            ["mock_invalid_code"] = invalidCode ? "true" : null,
        };

        var query = mockParams
            .Where(x => x.Value != null)
            .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value!)}");

        var authorizeUrl = $"{await RedirectTo(challenge)}&{string.Join('&', query)}";

        using var google = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var authorize = await google.GetAsync(authorizeUrl);

        var callback = new HttpRequestMessage(HttpMethod.Get, await RedirectTo(authorize));

        // O cookie de correlação do OAuth é Secure e o client de testes fala http,
        // então o CookieContainer o guarda mas nunca o reenvia — o browser real, em https, reenviaria.
        if (withCorrelationCookie && challenge.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies) callback.Headers.Add("Cookie", cookie.Split(';')[0]);
        }

        return await http.SendAsync(callback);
    }

    private static async Task<Uri> RedirectTo(HttpResponseMessage response)
    {
        if (response.Headers.Location != null) return response.Headers.Location;

        var body = await response.Content.ReadAsStringAsync();

        throw new InvalidOperationException($"Expected a redirect from {response.RequestMessage?.RequestUri}, got {(int)response.StatusCode}: {body}");
    }

    public async Task<OneOf<CheckSsoAvailabilityOut, ErrorOut>> CheckSsoAvailability(string email = "usuario@empresa.com.br")
    {
        var data = new CheckSsoAvailabilityIn { Email = email };
        var response = await http.PostAsJsonAsync("identity/sso/check-availability", data);
        return await response.Resolve<CheckSsoAvailabilityOut>();
    }

    public async Task<OneOf<GetSsoConfigurationOut, ErrorOut>> GetSsoConfiguration()
    {
        var response = await http.GetAsync("identity/sso/configuration");
        return await response.Resolve<GetSsoConfigurationOut>();
    }

    public async Task<OneOf<UpdateSsoConfigurationOut, ErrorOut>> UpdateSsoConfiguration(
        Guid ssoConfigurationId,
        SsoProviderType providerType = SsoProviderType.AzureAd,
        string authority = "https://login.microsoftonline.com/tenant-id/v2.0",
        string clientId = "00000000-0000-0000-0000-000000000000",
        string? clientSecret = null,
        bool isActive = true,
        bool requireSso = false
    ) {
        var data = new UpdateSsoConfigurationIn
        {
            ProviderType = providerType,
            Authority = authority,
            ClientId = clientId,
            ClientSecret = clientSecret,
            IsActive = isActive,
            RequireSso = requireSso,
        };
        var response = await http.PutAsJsonAsync($"identity/sso/configurations/{ssoConfigurationId}", data);
        return await response.Resolve<UpdateSsoConfigurationOut>();
    }
}

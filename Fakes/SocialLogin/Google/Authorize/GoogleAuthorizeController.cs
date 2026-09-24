using Estud.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Fakes.SocialLogin.Google.Authorize;

/// <summary>
/// Fake Google OAuth authorization endpoint.
/// Simulates Google's /o/oauth2/v2/auth by immediately redirecting with an authorization code.
/// Uses login_hint to resolve per-challenge config for parallel test safety.
/// </summary>
[ApiController]
public class GoogleAuthorizeController : ControllerBase
{
    [HttpGet("social-login/google/authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "login_hint")] string? loginHint,
        [FromQuery] string state,
        [FromQuery(Name = "fake_error")] string? error = null,
        [FromQuery(Name = "fake_invalid_code")] bool invalidCode = false,
        [FromQuery(Name = "fake_subject")] string? subject = null,
        [FromQuery(Name = "fake_email_verified")] bool emailVerified = true,
        [FromQuery(Name = "fake_name")] string? name = null,
        [FromQuery(Name = "fake_given_name")] string? givenName = null,
        [FromQuery(Name = "fake_family_name")] string? familyName = null)
    {
        if (error.HasValue())
        {
            return Redirect(QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>
            {
                ["error"] = error,
                ["state"] = state,
            }));
        }

        var code = Guid.NewGuid().ToString("N");

        if (!invalidCode)
        {
            GoogleFakeUsers.ByCode[code] = new GoogleFakeUser(
                loginHint,
                subject ?? Guid.NewGuid().ToString(),
                emailVerified,
                name,
                givenName,
                familyName
            );
        }

        var callback = QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = state,
        });

        return Redirect(callback);
    }
}

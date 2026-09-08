using Estud.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Mocks.SocialLogin.Google.Authorize;

/// <summary>
/// Mock Google OAuth authorization endpoint.
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
        [FromQuery(Name = "mock_error")] string? error = null,
        [FromQuery(Name = "mock_invalid_code")] bool invalidCode = false,
        [FromQuery(Name = "mock_subject")] string? subject = null,
        [FromQuery(Name = "mock_email_verified")] bool emailVerified = true,
        [FromQuery(Name = "mock_name")] string? name = null,
        [FromQuery(Name = "mock_given_name")] string? givenName = null,
        [FromQuery(Name = "mock_family_name")] string? familyName = null)
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
            GoogleMockUsers.ByCode[code] = new GoogleMockUser(
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

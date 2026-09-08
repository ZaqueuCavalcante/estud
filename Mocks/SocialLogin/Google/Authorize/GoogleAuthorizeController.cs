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
        [FromQuery(Name = "login_hint")] string loginHint,
        [FromQuery] string state)
    {
        var code = Guid.NewGuid().ToString("N");

        GoogleMockUsers.ByCode[code] = new GoogleMockUser(loginHint, Guid.NewGuid().ToString());

        var callback = QueryHelpers.AddQueryString(redirectUri, new Dictionary<string, string?>
        {
            ["code"] = code,
            ["state"] = state,
        });

        return Redirect(callback);
    }
}

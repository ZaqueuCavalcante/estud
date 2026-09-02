using Estud.Back.Auth.Schemes;
using Microsoft.AspNetCore.Authentication;

namespace Estud.Back.Features.Identity.SocialLoginChallenge;

[ApiController, EnableRateLimiting(RateLimitingConfigs.SensitivePolicy)]
public class SocialLoginChallengeController(FrontendSettings frontendSettings) : ControllerBase
{
    /// <summary>
    /// Social Login Challenge 🔓
    /// </summary>
    /// <remarks>
    /// Redirects to the social login provider (Google) for authentication.
    /// This is a browser redirect endpoint, not a JSON API.
    /// </remarks>
    [HttpGet("identity/social-login/challenge/{provider}")]
    public IActionResult Challenge(string provider)
    {
        Enum.TryParse(provider, ignoreCase: true, out SocialLoginProvider loginProvider);

        var schemeName = loginProvider switch
        {
            SocialLoginProvider.Google => SocialLoginScheme.GoogleScheme,
            _ => null,
        };

        if (schemeName == null) return Redirect($"{frontendSettings.Url}?social_login_error={nameof(SocialLoginFailed)}");

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/home",
        };

        return Challenge(properties, schemeName);
    }
}

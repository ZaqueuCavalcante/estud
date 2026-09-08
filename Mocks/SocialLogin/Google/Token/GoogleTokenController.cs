using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.SocialLogin.Google.Token;

/// <summary>
/// Mock Google OAuth token endpoint.
/// Exchanges authorization code for access token.
/// </summary>
[ApiController]
public class GoogleTokenController : ControllerBase
{
    [HttpPost("social-login/google/token")]
    public IActionResult Token([FromForm] string code)
    {
        if (!GoogleMockUsers.ByCode.TryRemove(code, out var user)) return BadRequest(new { error = "invalid_grant" });

        var accessToken = Guid.NewGuid().ToString("N");

        GoogleMockUsers.ByAccessToken[accessToken] = user;

        return Ok(new
        {
            expires_in = 3600,
            token_type = "Bearer",
            access_token = accessToken,
        });
    }
}

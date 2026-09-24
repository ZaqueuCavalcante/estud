using Microsoft.AspNetCore.Mvc;

namespace Estud.Fakes.SocialLogin.Google.Token;

/// <summary>
/// Fake Google OAuth token endpoint.
/// Exchanges authorization code for access token.
/// </summary>
[ApiController]
public class GoogleTokenController : ControllerBase
{
    [HttpPost("social-login/google/token")]
    public IActionResult Token([FromForm] string code)
    {
        if (!GoogleFakeUsers.ByCode.TryRemove(code, out var user)) return BadRequest(new { error = "invalid_grant" });

        var accessToken = Guid.NewGuid().ToString("N");

        GoogleFakeUsers.ByAccessToken[accessToken] = user;

        return Ok(new
        {
            expires_in = 3600,
            token_type = "Bearer",
            access_token = accessToken,
        });
    }
}

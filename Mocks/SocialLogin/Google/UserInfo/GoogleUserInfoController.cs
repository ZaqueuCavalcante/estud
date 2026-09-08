using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.SocialLogin.Google.UserInfo;

/// <summary>
/// Mock Google OAuth userinfo endpoint.
/// Returns the claims of the user bound to the access token.
/// </summary>
[ApiController]
public class GoogleUserInfoController : ControllerBase
{
    [HttpGet("social-login/google/userinfo")]
    public IActionResult UserInfo()
    {
        var accessToken = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        if (!GoogleMockUsers.ByAccessToken.TryGetValue(accessToken, out var user)) return Unauthorized();

        return Ok(new
        {
            sub = user.Subject,
            email = user.Email,
            email_verified = true,
        });
    }
}

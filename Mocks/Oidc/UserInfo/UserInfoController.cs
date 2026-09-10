using Microsoft.AspNetCore.Mvc;

namespace Estud.Mocks.Oidc.UserInfo;

/// <summary>
/// UserInfo endpoint - returns authenticated user claims.
/// Uses per-access-token session state for parallel test safety.
/// </summary>
[ApiController]
public class UserInfoController : ControllerBase
{
    [HttpGet("oidc/connect/userinfo")]
    public IActionResult UserInfo()
    {
        var accessToken = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        if (!OidcMockUsers.ByAccessToken.TryGetValue(accessToken, out var user)) return Unauthorized();

        return Ok(new
        {
            name = user.Name,
            email = user.Email,
            sub = user.UserInfoSubject ?? user.Subject,
            email_verified = user.EmailVerified,
        });
    }
}

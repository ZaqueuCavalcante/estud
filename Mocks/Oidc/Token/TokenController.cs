using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Estud.Mocks.Oidc.Token;

/// <summary>
/// Token endpoint - exchanges authorization code for tokens.
/// Uses per-auth-code captured state for parallel test safety.
/// Supports producing malicious tokens for security testing.
/// </summary>
[ApiController]
public class TokenController : ControllerBase
{
    [HttpPost("oidc/connect/token")]
    public IActionResult Token([FromForm] string code)
    {
        if (!OidcMockUsers.ByCode.TryRemove(code, out var user)) return BadRequest(new { error = "invalid_grant" });

        var accessToken = Guid.NewGuid().ToString("N");

        OidcMockUsers.ByAccessToken[accessToken] = user;

        return Ok(new
        {
            expires_in = 3600,
            token_type = "Bearer",
            access_token = accessToken,
            id_token = BuildIdToken(user),
        });
    }

    private string BuildIdToken(OidcMockUser user)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Subject),
            new(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(now).ToString(), ClaimValueTypes.Integer64),
        };

        if (user.Nonce != null) claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, user.Nonce));
        if (user.Email != null) claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        var token = new JwtSecurityToken(
            issuer: $"{Request.Scheme}://{Request.Host}/oidc",
            audience: user.ClientId,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: OidcMockKeys.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

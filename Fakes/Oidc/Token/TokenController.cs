using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Fakes.Oidc.Token;

/// <summary>
/// Token endpoint - exchanges authorization code for tokens.
/// Uses per-auth-code captured state for parallel test safety.
/// Supports producing malicious tokens for security testing.
/// </summary>
[ApiController]
public class TokenController : ControllerBase
{
    [HttpPost("oidc/connect/token")]
    public IActionResult Token([FromForm] string code, [FromForm(Name = "code_verifier")] string? codeVerifier = null)
    {
        if (!OidcFakeUsers.ByCode.TryRemove(code, out var user)) return BadRequest(new { error = "invalid_grant" });

        if (user.CodeChallenge != null && !MatchesCodeChallenge(codeVerifier, user.CodeChallenge))
        {
            return BadRequest(new { error = "invalid_grant" });
        }

        var accessToken = Guid.NewGuid().ToString("N");

        OidcFakeUsers.ByAccessToken[accessToken] = user;

        return Ok(new
        {
            expires_in = 3600,
            token_type = "Bearer",
            access_token = accessToken,
            id_token = BuildIdToken(user),
        });
    }

    private static bool MatchesCodeChallenge(string? codeVerifier, string codeChallenge)
    {
        if (codeVerifier == null) return false;

        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));

        return WebEncoders.Base64UrlEncode(hash) == codeChallenge;
    }

    private string BuildIdToken(OidcFakeUser user)
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
            signingCredentials: OidcFakeKeys.SigningCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

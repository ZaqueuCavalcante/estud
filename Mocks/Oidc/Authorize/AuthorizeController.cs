using Estud.Back.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Estud.Mocks.Oidc.Authorize;

/// <summary>
/// Authorization endpoint - initiates the login flow.
/// Simulates a successful IdP authentication by immediately redirecting back with an authorization code.
/// Uses login_hint to resolve per-challenge config, eliminating global state races in parallel tests.
/// </summary>
[ApiController]
public class AuthorizeController : ControllerBase
{
    [HttpGet("oidc/connect/authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "client_id")] string clientId,
        [FromQuery(Name = "login_hint")] string? loginHint,
        [FromQuery] string state,
        [FromQuery] string? nonce = null,
        [FromQuery(Name = "mock_error")] string? error = null,
        [FromQuery(Name = "mock_invalid_code")] bool invalidCode = false,
        [FromQuery(Name = "mock_subject")] string? subject = null,
        [FromQuery(Name = "mock_email")] string? email = null,
        [FromQuery(Name = "mock_no_email")] bool noEmail = false,
        [FromQuery(Name = "mock_email_verified")] bool emailVerified = true,
        [FromQuery(Name = "mock_name")] string? name = null,
        [FromQuery(Name = "mock_userinfo_subject")] string? userInfoSubject = null)
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
            OidcMockUsers.ByCode[code] = new OidcMockUser(
                noEmail ? null : email ?? loginHint,
                subject ?? Guid.NewGuid().ToString(),
                clientId,
                nonce,
                emailVerified,
                name,
                userInfoSubject
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

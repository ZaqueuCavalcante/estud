using Microsoft.AspNetCore.Authentication;

namespace Estud.Back.Features.Identity.SsoChallenge;

[ApiController, EnableRateLimiting(RateLimitingConfigs.SensitivePolicy)]
public class SsoChallengeController(SsoChallengeService service, FrontendSettings frontendSettings) : ControllerBase
{
    /// <summary>
    /// SSO Challenge 🔓
    /// </summary>
    /// <remarks>
    /// Redireciona o usuário para o provedor de identidade configurado para o domínio do email.
    /// Este é um endpoint de redirect de browser, não uma API JSON.
    /// Deve ser chamado depois que o check-availability indicar que o domínio tem SSO.
    /// </remarks>
    [HttpGet("identity/sso/challenge")]
    public async Task<IActionResult> Challenge([FromQuery] string? email = null)
    {
        var result = await service.GetScheme(email);
        if (result.IsError) return Redirect($"{frontendSettings.Url}?sso_error={result.Error.Code}");

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/home",
        };

        properties.Items["login_hint"] = email;

        return Challenge(properties, result.Success);
    }
}

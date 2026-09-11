namespace Estud.Back.Features.Identity.VerifySsoDomain;

[ApiController, Authorize(Policies.VerifySsoDomain), EnableRateLimiting(RateLimitingConfigs.SensitivePolicy)]
public class VerifySsoDomainController(VerifySsoDomainService service) : ControllerBase
{
    /// <summary>
    /// Verificar domínio SSO
    /// </summary>
    /// <remarks>
    /// Comprova que a instituição controla o domínio, consultando no DNS o registro TXT
    /// `_estud-challenge.{domínio}` com o valor `estud-domain-verification={token}`,
    /// informados na configuração SSO. Só domínios verificados roteiam login via SSO
    /// e aplicam o SSO obrigatório.
    /// </remarks>
    [HttpPost("identity/sso/configurations/{ssoConfigurationId}/domains/verify")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Verify([FromRoute] Guid ssoConfigurationId, [FromBody] VerifySsoDomainIn data)
    {
        var result = await service.Verify(ssoConfigurationId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<VerifySsoDomainIn>;
internal class ResponseExamples : ExamplesProvider<VerifySsoDomainOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidSsoDomain,
    SsoConfigurationNotFound,
    SsoDomainNotFound,
    SsoDomainVerifiedByAnotherInstitution,
    SsoDomainDnsLookupFailed,
    SsoDomainVerificationRecordNotFound
>;

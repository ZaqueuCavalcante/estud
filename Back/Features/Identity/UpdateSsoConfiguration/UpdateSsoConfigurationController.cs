namespace Estud.Back.Features.Identity.UpdateSsoConfiguration;

[ApiController, Authorize(Policies.UpdateSsoConfiguration)]
public class UpdateSsoConfigurationController(UpdateSsoConfigurationService service) : ControllerBase
{
    /// <summary>
    /// Atualizar configuração SSO
    /// </summary>
    /// <remarks>
    /// Atualiza a configuração de Single Sign-On da instituição.
    /// </remarks>
    [HttpPut("identity/sso/configurations/{ssoConfigurationId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] Guid ssoConfigurationId, [FromBody] UpdateSsoConfigurationIn data)
    {
        var result = await service.Update(ssoConfigurationId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateSsoConfigurationIn>;
internal class ResponseExamples : ExamplesProvider<UpdateSsoConfigurationOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    SsoConfigurationNotFound,
    InvalidSsoProviderType,
    InvalidSsoAuthority,
    InvalidSsoClientId,
    SsoAuthorityMustBeHttps,
    SsoAuthorityHasUserInfo,
    SsoAuthorityLocalhostNotAllowed,
    SsoAuthorityLoopbackNotAllowed,
    SsoAuthorityPrivateIpNotAllowed,
    SsoAuthorityLinkLocalNotAllowed
>;

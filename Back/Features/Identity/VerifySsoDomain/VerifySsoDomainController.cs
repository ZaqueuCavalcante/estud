namespace Estud.Back.Features.Identity.VerifySsoDomain;

[ApiController, Authorize(Policies.VerifySsoDomain)]
public class VerifySsoDomainController(VerifySsoDomainService service) : ControllerBase
{
    /// <summary>
    /// Verificar domínio SSO
    /// </summary>
    /// <remarks>
    /// Consulta o registro TXT de verificação no DNS do domínio e, se ele conferir com o token
    /// gerado na criação da configuração, marca o domínio como verificado.
    /// Enquanto o domínio não estiver verificado o SSO não roteia nenhum login para ele.
    /// </remarks>
    [HttpPost("identity/sso/domains/{domain}/verify")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Verify([FromRoute] string domain)
    {
        var result = await service.Verify(domain);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<VerifySsoDomainOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    SsoDomainNotFound,
    SsoDomainVerificationFailed
>;

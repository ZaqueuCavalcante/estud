namespace Estud.Back.Features.Webhooks.GetWebhookCalls;

[ApiController, Authorize(Policies.GetWebhookCalls)]
public class GetWebhookCallsController(GetWebhookCallsService service) : ControllerBase
{
    /// <summary>
    /// Chamadas de webhook
    /// </summary>
    /// <remarks>
    /// Retorna a lista paginada de chamadas de uma inscrição de webhook, da mais recente para a mais antiga.
    /// Pode ser filtrada por status.
    /// </remarks>
    [HttpGet("webhooks/{subscriptionId}/calls")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int subscriptionId, [FromQuery] GetWebhookCallsIn data)
    {
        var result = await service.Get(subscriptionId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetWebhookCallsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    WebhookSubscriptionNotFound
>;

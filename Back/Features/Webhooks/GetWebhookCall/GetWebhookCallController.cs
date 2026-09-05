namespace Estud.Back.Features.Webhooks.GetWebhookCall;

[ApiController, Authorize(Policies.GetWebhookCall)]
public class GetWebhookCallController(GetWebhookCallService service) : ControllerBase
{
    /// <summary>
    /// Detalhes de chamada de webhook
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes de uma chamada de webhook, incluindo o payload do evento, a inscrição de destino e todas as tentativas de entrega.
    /// </remarks>
    [HttpGet("webhooks/calls/{callId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int callId)
    {
        var result = await service.Get(callId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetWebhookCallOut>;
internal class ErrorsExamples : ErrorExamplesProvider<WebhookCallNotFound>;

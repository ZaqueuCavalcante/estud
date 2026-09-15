namespace Estud.Back.Features.Webhooks.RetryWebhookCall;

[ApiController, Authorize(Policies.RetryWebhookCall)]
public class RetryWebhookCallController(RetryWebhookCallService service) : ControllerBase
{
    /// <summary>
    /// Reprocessar chamada de webhook
    /// </summary>
    /// <remarks>
    /// Agenda uma nova tentativa de entrega para uma chamada de webhook que falhou.
    /// Apenas chamadas com status de erro podem ser reprocessadas. O payload enviado é o mesmo da chamada original.
    /// </remarks>
    [HttpPost("webhooks/calls/{callId}/retry")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Retry([FromRoute] int callId)
    {
        var result = await service.Retry(callId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    WebhookCallNotFound,
    WebhookCallCannotBeRetried
>;

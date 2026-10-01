namespace Estud.Back.Features.Webhooks.UpdateWebhookSubscriptionRetryConfigs;

[ApiController, Authorize(Policies.UpdateWebhookSubscriptionRetryConfigs)]
public class UpdateWebhookSubscriptionRetryConfigsController(UpdateWebhookSubscriptionRetryConfigsService service) : ControllerBase
{
    /// <summary>
    /// Editar configurações de retentativa do webhook
    /// </summary>
    /// <remarks>
    /// Atualiza as configurações de retentativa automática de uma inscrição de webhook.
    /// Quando uma chamada falha, ela é retentada até MaxRetries vezes, aguardando um intervalo
    /// calculado a partir de BaseDelaySeconds e da BackoffStrategy entre cada tentativa.
    /// </remarks>
    [HttpPut("webhooks/subscriptions/{subscriptionId}/retry-configs")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int subscriptionId, [FromBody] UpdateWebhookSubscriptionRetryConfigsIn data)
    {
        var result = await service.Update(subscriptionId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateWebhookSubscriptionRetryConfigsIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidWebhookMaxRetries,
    InvalidWebhookBaseDelaySeconds,
    InvalidWebhookBackoffStrategy,
    WebhookSubscriptionNotFound
>;

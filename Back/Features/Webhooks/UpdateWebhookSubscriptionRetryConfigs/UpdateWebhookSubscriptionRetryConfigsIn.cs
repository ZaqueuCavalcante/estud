namespace Estud.Back.Features.Webhooks.UpdateWebhookSubscriptionRetryConfigs;

public class UpdateWebhookSubscriptionRetryConfigsIn : IApiDto<UpdateWebhookSubscriptionRetryConfigsIn>
{
    /// <summary>
    /// Número máximo de retentativas após a primeira chamada falhar (0 a 5).
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Intervalo base, em segundos, usado no cálculo do backoff (0 a 1800).
    /// </summary>
    public int BaseDelaySeconds { get; set; }

    public BackoffStrategy? BackoffStrategy { get; set; }

    public static IEnumerable<(string, UpdateWebhookSubscriptionRetryConfigsIn)> GetExamples() =>
    [
        ("Exemplo",
        new UpdateWebhookSubscriptionRetryConfigsIn
        {
            MaxRetries = 3,
            BaseDelaySeconds = 10,
            BackoffStrategy = Domain.Enums.BackoffStrategy.Exponential,
        }),
    ];
}

namespace Estud.Back.Features.Webhooks.GetWebhookCalls;

public class GetWebhookCallsIn : IApiDto<GetWebhookCallsIn>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public WebhookCallStatus? Status { get; set; }

    public static IEnumerable<(string, GetWebhookCallsIn)> GetExamples() =>
    [
        ("Exemplo", new GetWebhookCallsIn() { }),
        ("Somente com erro", new GetWebhookCallsIn() { Status = WebhookCallStatus.Error }),
    ];
}

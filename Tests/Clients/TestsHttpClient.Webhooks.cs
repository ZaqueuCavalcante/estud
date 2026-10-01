using System.Net.Http.Json;
using Estud.Back.Features.Webhooks.GetWebhookCall;
using Estud.Back.Features.Webhooks.GetWebhookCalls;
using Estud.Back.Features.Webhooks.GetWebhookSubscription;
using Estud.Back.Features.Webhooks.GetWebhookSubscriptions;
using Estud.Back.Features.Webhooks.CreateWebhookSubscription;
using Estud.Back.Features.Webhooks.UpdateWebhookSubscription;
using Estud.Back.Features.Webhooks.UpdateWebhookSubscriptionRetryConfigs;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<CreateWebhookSubscriptionOut, ErrorOut>> CreateWebhookSubscription(
        string name = "Aluno criado",
        string url = "https://webhook.site/my-webhook",
        List<WebhookEventType>? events = null,
        Dictionary<string, string>? customHeaders = null
    ) {
        var data = new CreateWebhookSubscriptionIn
        {
            Name = name,
            Url = url,
            Events = events ?? [WebhookEventType.StudentCreated],
            CustomHeaders = customHeaders ?? [],
        };
        var response = await http.PostAsJsonAsync("webhooks/subscriptions", data);
        return await response.Resolve<CreateWebhookSubscriptionOut>();
    }

    public async Task<OneOf<GetWebhookSubscriptionsOut, ErrorOut>> GetWebhookSubscriptions()
    {
        var response = await http.GetAsync("webhooks/subscriptions");
        return await response.Resolve<GetWebhookSubscriptionsOut>();
    }

    public async Task<OneOf<GetWebhookSubscriptionOut, ErrorOut>> GetWebhookSubscription(int subscriptionId)
    {
        var response = await http.GetAsync($"webhooks/subscriptions/{subscriptionId}");
        return await response.Resolve<GetWebhookSubscriptionOut>();
    }

    public async Task<OneOf<GetWebhookCallsOut, ErrorOut>> GetWebhookCalls(
        int subscriptionId,
        int page = 1,
        int pageSize = 20,
        WebhookCallStatus? status = null
    ) {
        var data = new GetWebhookCallsIn
        {
            Page = page,
            PageSize = pageSize,
            Status = status,
        };

        var response = await http.GetAsync($"webhooks/{subscriptionId}/calls".AddQueryString(data));
        return await response.Resolve<GetWebhookCallsOut>();
    }

    public async Task<OneOf<GetWebhookCallOut, ErrorOut>> GetWebhookCall(int callId)
    {
        var response = await http.GetAsync($"webhooks/calls/{callId}");
        return await response.Resolve<GetWebhookCallOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> RetryWebhookCall(int callId)
    {
        var response = await http.PostAsync($"webhooks/calls/{callId}/retry", null);
        return await response.Resolve<SuccessOut>();
    }

    public async Task<OneOf<UpdateWebhookSubscriptionOut, ErrorOut>> UpdateWebhookSubscription(
        int webhookSubscriptionId,
        string name = "Aluno criado",
        string url = "https://webhook.site/my-webhook",
        bool isActive = true,
        List<WebhookEventType>? events = null,
        Dictionary<string, string>? customHeaders = null
    ) {
        var data = new UpdateWebhookSubscriptionIn
        {
            Name = name,
            Url = url,
            IsActive = isActive,
            Events = events ?? [WebhookEventType.StudentCreated],
            CustomHeaders = customHeaders ?? [],
        };
        var response = await http.PutAsJsonAsync($"webhooks/subscriptions/{webhookSubscriptionId}", data);
        return await response.Resolve<UpdateWebhookSubscriptionOut>();
    }

    public async Task<OneOf<SuccessOut, ErrorOut>> UpdateWebhookSubscriptionRetryConfigs(
        int webhookSubscriptionId,
        int maxRetries = 3,
        int baseDelaySeconds = 10,
        BackoffStrategy? backoffStrategy = BackoffStrategy.Exponential
    ) {
        var data = new UpdateWebhookSubscriptionRetryConfigsIn
        {
            MaxRetries = maxRetries,
            BaseDelaySeconds = baseDelaySeconds,
            BackoffStrategy = backoffStrategy,
        };
        var response = await http.PutAsJsonAsync($"webhooks/subscriptions/{webhookSubscriptionId}/retry-configs", data);
        return await response.Resolve<SuccessOut>();
    }
}

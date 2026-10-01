using Estud.Back.Domain.Webhooks;

namespace Estud.Back.Features.Webhooks.GetWebhookSubscription;

public static class GetWebhookSubscriptionMapper
{
    extension(WebhookSubscription subscription)
    {
        public GetWebhookSubscriptionOut ToGetWebhookSubscriptionOut()
        {
            return new()
            {
                Id = subscription.Id,
                Name = subscription.Name,
                Url = subscription.Url,
                IsActive = subscription.IsActive,
                Events = subscription.Events,
                CustomHeaders = subscription.CustomHeaders,
                MaxRetries = subscription.MaxRetries,
                BaseDelaySeconds = subscription.BaseDelaySeconds,
                BackoffStrategy = subscription.BackoffStrategy,
                CreatedAt = subscription.CreatedAt,
            };
        }
    }
}

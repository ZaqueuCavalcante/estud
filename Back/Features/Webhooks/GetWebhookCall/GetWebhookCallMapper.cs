using Estud.Back.Domain.Webhooks;

namespace Estud.Back.Features.Webhooks.GetWebhookCall;

public static class GetWebhookCallMapper
{
    extension(WebhookCall call)
    {
        public GetWebhookCallOut ToGetWebhookCallOut()
        {
            return new()
            {
                Id = call.Id,
                EventType = call.EventType,
                Status = call.Status,
                AttemptsCount = call.AttemptsCount,
                CreatedAt = call.CreatedAt,
                Payload = call.Payload,
                Request = new GetWebhookCallRequestOut
                {
                    Method = "POST",
                    Url = call.Subscription.Url,
                    Headers = new Dictionary<string, string>(call.Subscription.CustomHeaders)
                    {
                        ["Content-Type"] = "application/json; charset=utf-8",
                    },
                    Body = call.Payload,
                },
                Subscription = new GetWebhookCallSubscriptionOut
                {
                    Id = call.Subscription.Id,
                    Name = call.Subscription.Name,
                    Url = call.Subscription.Url,
                    IsActive = call.Subscription.IsActive,
                },
                Attempts = [.. call.Attempts
                    .OrderByDescending(x => x.CreatedAt)
                    .ThenByDescending(x => x.Id)
                    .Select(x => x.ToGetWebhookCallAttemptOut())],
            };
        }
    }

    extension(WebhookCallAttempt attempt)
    {
        public GetWebhookCallAttemptOut ToGetWebhookCallAttemptOut()
        {
            return new()
            {
                Id = attempt.Id,
                Status = attempt.Status,
                StatusCode = attempt.StatusCode,
                Response = attempt.Response,
                DurationMs = attempt.DurationMs,
                CreatedAt = attempt.CreatedAt,
            };
        }
    }
}

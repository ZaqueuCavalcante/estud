using Estud.Back.Domain.Classes;
using Estud.Back.Domain.Webhooks;
using Estud.Back.Features.Webhooks.CallWebhooks;

namespace Estud.Back.Features.Teachers.CreateClassActivity;

public class ClassActivityPublishedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<ClassActivityPublishedDomainEvent>
{
    public async Task Handle(int institutionId, string eventUid, ClassActivityPublishedDomainEvent evt)
    {
        var activity = await ctx.ClassActivities.AsNoTracking()
            .Where(x => x.Uid == evt.Uid)
            .Select(x => new { x.Id, x.ClassId, x.Title, x.ActivityType, x.DueDate })
            .FirstAsync();

        ctx.AddCommand(institutionId, new CreateNewClassActivityNotificationCommand(activity.Id));

        var subscriptions = await ctx.WebhookSubscriptions
            .Where(x => x.InstitutionId == institutionId && x.IsActive)
            .Select(x => new { x.Id, x.Events, x.MaxRetries, x.BackoffStrategy, x.BaseDelaySeconds }).ToListAsync() ?? [];

        foreach (var subscription in subscriptions.Where(x => x.Events.Contains(WebhookEventType.ClassActivityPublished)))
        {
            var data = new
            {
                activity.Id,
                activity.Title,
                activity.ClassId,
                Type = activity.ActivityType,
                DueDate = activity.DueDate.ToString("yyyy-MM-dd"),
            };

            var webhookCall = new WebhookCall(institutionId, subscription.Id, eventUid, data, WebhookEventType.ClassActivityPublished);
            ctx.Add(webhookCall);
            ctx.AddCommand(
                institutionId,
                new CallWebhookCommand(webhookCall.Uid),
                maxRetries: subscription.MaxRetries,
                backoffStrategy: subscription.BackoffStrategy,
                baseDelaySeconds: subscription.BaseDelaySeconds
            );
        }
    }
}

using Estud.Back.Domain.Classes;
using Estud.Back.Domain.Webhooks;
using Estud.Back.Features.Webhooks.CallWebhooks;

namespace Estud.Back.Features.Teachers.CreateClassActivity;

public class ClassActivityCreatedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<ClassActivityCreatedDomainEvent>
{
    public async Task Handle(int institutionId, int eventId, ClassActivityCreatedDomainEvent evt)
    {
        var activity = await ctx.ClassActivities.AsNoTracking()
            .Where(x => x.Uid == evt.Uid)
            .Select(x => new { x.Id, x.ClassId, x.Title, x.Description, x.ActivityType, x.DueDate })
            .FirstAsync();

        ctx.AddCommand(institutionId, new CreateNewClassActivityNotificationCommand(activity.Id));

        var subscriptions = await ctx.WebhookSubscriptions
            .Where(x => x.InstitutionId == institutionId && x.IsActive)
            .Select(x => new { x.Id, x.Events }).ToListAsync();

        foreach (var subscription in subscriptions.Where(x => x.Events.Contains(WebhookEventType.ClassActivityCreated)))
        {
            var data = new
            {
                activity.Id,
                activity.Title,
                activity.ClassId,
                activity.Description,
                Type = activity.ActivityType,
                DueDate = activity.DueDate.ToString("yyyy-MM-dd"),
            };

            var webhookCall = new WebhookCall(institutionId, subscription.Id, data, WebhookEventType.ClassActivityCreated);
            ctx.Add(webhookCall);
            ctx.AddCommand(institutionId, new CallWebhookCommand(webhookCall.Uid));
        }
    }
}

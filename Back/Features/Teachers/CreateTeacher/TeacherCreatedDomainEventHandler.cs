using Estud.Back.Emails;
using Estud.Back.Domain.Identity;
using Estud.Back.Domain.Teachers;
using Estud.Back.Domain.Webhooks;
using Estud.Back.Features.Webhooks.CallWebhooks;

namespace Estud.Back.Features.Teachers.CreateTeacher;

public class TeacherCreatedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<TeacherCreatedDomainEvent>
{
    public async Task Handle(int institutionId, string eventUid, TeacherCreatedDomainEvent evt)
    {
        var teacher = await ctx.Teachers.Where(x => x.Uid == evt.Uid).Select(x => new { x.UserId, x.Name }).FirstAsync();
        var user = await ctx.Users.FirstAsync(x => x.Id == teacher.UserId);
        var institutionName = await ctx.Institutions.Where(x => x.Id == institutionId).Select(x => x.Name).FirstAsync();

        var magicLink = new MagicLink(user, TimeSpan.FromDays(7));
        ctx.Add(magicLink);
        ctx.AddCommand(institutionId, new SendInviteEmailCommand(user.Email!, teacher.Name, institutionName, "professor", magicLink.Id), maxRetries: 1);

        var subscriptions = await ctx.WebhookSubscriptions
            .Where(x => x.InstitutionId == institutionId && x.IsActive)
            .Select(x => new { x.Id, x.Events, x.MaxRetries, x.BackoffStrategy, x.BaseDelaySeconds }).ToListAsync() ?? [];

        foreach (var subscription in subscriptions.Where(x => x.Events.Contains(WebhookEventType.TeacherCreated)))
        {
            var webhookCall = new WebhookCall(institutionId, subscription.Id, eventUid, new { teacher.Name, user.Email }, WebhookEventType.TeacherCreated);
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

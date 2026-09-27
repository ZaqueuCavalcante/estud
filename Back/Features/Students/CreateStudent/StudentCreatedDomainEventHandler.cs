using Estud.Back.Emails;
using Estud.Back.Domain.Identity;
using Estud.Back.Domain.Students;
using Estud.Back.Domain.Webhooks;
using Estud.Back.Features.Webhooks.CallWebhooks;

namespace Estud.Back.Features.Students.CreateStudent;

public class StudentCreatedDomainEventHandler(EstudDbContext ctx) : IDomainEventHandler<StudentCreatedDomainEvent>
{
    public async Task Handle(int institutionId, int eventId, StudentCreatedDomainEvent evt)
    {
        var student = await ctx.Students.Where(x => x.Uid == evt.Uid).Select(x => new { x.UserId, x.Name }).FirstAsync();
        var user = await ctx.Users.FirstAsync(x => x.Id == student.UserId);
        var institutionName = await ctx.Institutions.Where(x => x.Id == institutionId).Select(x => x.Name).FirstAsync();

        var magicLink = new MagicLink(user, TimeSpan.FromDays(7));
        ctx.Add(magicLink);
        ctx.AddCommand(institutionId, new SendInviteEmailCommand(user.Email!, student.Name, institutionName, "aluno", magicLink.Id), maxRetries: 1);

        var subscriptions = await ctx.WebhookSubscriptions
            .Where(x => x.InstitutionId == institutionId && x.IsActive)
            .Select(x => new { x.Id, x.Events }).ToListAsync() ?? [];

        foreach (var subscription in subscriptions.Where(x => x.Events.Contains(WebhookEventType.StudentCreated)))
        {
            var webhookCall = new WebhookCall(institutionId, subscription.Id, new { student.Name, user.Email }, WebhookEventType.StudentCreated);
            ctx.Add(webhookCall);
            ctx.AddCommand(institutionId, new CallWebhookCommand(webhookCall.Uid));
        }
    }
}

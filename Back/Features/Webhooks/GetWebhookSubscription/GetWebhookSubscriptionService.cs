namespace Estud.Back.Features.Webhooks.GetWebhookSubscription;

public class GetWebhookSubscriptionService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetWebhookSubscriptionOut, EstudError>> Get(int subscriptionId)
    {
        var subscription = await ctx.WebhookSubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.InstitutionId == ctx.RequestUser.InstitutionId && x.Id == subscriptionId);
        if (subscription == null) return WebhookSubscriptionNotFound.I;

        return subscription.ToGetWebhookSubscriptionOut();
    }
}

namespace Estud.Back.Features.Webhooks.GetWebhookCall;

public class GetWebhookCallService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetWebhookCallOut, EstudError>> Get(int callId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var call = await ctx.WebhookCalls.AsNoTracking()
            .Include(x => x.Subscription)
            .Include(x => x.Attempts)
            .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == callId);
        if (call == null) return WebhookCallNotFound.I;

        return call.ToGetWebhookCallOut();
    }
}

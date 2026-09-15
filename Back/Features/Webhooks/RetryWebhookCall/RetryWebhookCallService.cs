using Estud.Back.Features.Webhooks.CallWebhooks;

namespace Estud.Back.Features.Webhooks.RetryWebhookCall;

public class RetryWebhookCallService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Retry(int callId)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var call = await ctx.WebhookCalls.FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == callId);
        if (call == null) return WebhookCallNotFound.I;

        if (call.Status != WebhookCallStatus.Error) return WebhookCallCannotBeRetried.I;

        call.Retry();
        ctx.AddCommand(institutionId, new CallWebhookCommand(call.Uid));
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

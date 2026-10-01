namespace Estud.Back.Features.Webhooks.UpdateWebhookSubscriptionRetryConfigs;

public class UpdateWebhookSubscriptionRetryConfigsService(EstudDbContext ctx) : IEstudService
{
    private class Validator : AbstractValidator<UpdateWebhookSubscriptionRetryConfigsIn>
    {
        public Validator()
        {
            RuleFor(x => x.MaxRetries).InclusiveBetween(0, 5).WithError(InvalidWebhookMaxRetries.I);

            RuleFor(x => x.BaseDelaySeconds).InclusiveBetween(0, 30).WithError(InvalidWebhookBaseDelaySeconds.I);

            RuleFor(x => x.BackoffStrategy).NotNull().WithError(InvalidWebhookBackoffStrategy.I);
            RuleFor(x => x.BackoffStrategy).IsInEnum().WithError(InvalidWebhookBackoffStrategy.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<EstudSuccess, EstudError>> Update(int subscriptionId, UpdateWebhookSubscriptionRetryConfigsIn data)
    {
        if (V.Run(data, out var error)) return error;

        var subscription = await ctx.WebhookSubscriptions
            .FirstOrDefaultAsync(x => x.InstitutionId == ctx.RequestUser.InstitutionId && x.Id == subscriptionId);
        if (subscription == null) return WebhookSubscriptionNotFound.I;

        subscription.UpdateRetryConfigs(data.MaxRetries, data.BaseDelaySeconds, data.BackoffStrategy!.Value);
        await ctx.SaveChangesAsync();

        return EstudSuccess.I;
    }
}

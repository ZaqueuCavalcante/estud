namespace Estud.Back.Features.Webhooks.GetWebhookCalls;

public class GetWebhookCallsService(EstudDbContext ctx) : IEstudService
{
    public async Task<OneOf<GetWebhookCallsOut, EstudError>> Get(int subscriptionId, GetWebhookCallsIn data)
    {
        var institutionId = ctx.RequestUser.InstitutionId;

        var subscriptionExists = await ctx.WebhookSubscriptions
            .AnyAsync(x => x.InstitutionId == institutionId && x.Id == subscriptionId);
        if (!subscriptionExists) return WebhookSubscriptionNotFound.I;

        var query = ctx.WebhookCalls.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.WebhookSubscriptionId == subscriptionId);

        if (data.Status is not null)
            query = query.Where(x => x.Status == data.Status);

        var total = await query.CountAsync();

        var calls = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((data.Page - 1) * data.PageSize)
            .Take(data.PageSize)
            .ToListAsync();

        var items = calls.ConvertAll(x => x.ToGetWebhookCallsItemOut());

        return new GetWebhookCallsOut
        {
            Total = total,
            Page = data.Page,
            PageSize = data.PageSize,
            Items = items,
        };
    }
}

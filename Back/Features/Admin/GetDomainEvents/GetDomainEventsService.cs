namespace Estud.Back.Features.Admin.GetDomainEvents;

public class GetDomainEventsService(EstudDbContext ctx) : IEstudService
{
    private const int MaxPageSize = 100;

    public async Task<GetDomainEventsOut> Get(GetDomainEventsIn query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var eventsQuery = ctx.DomainEvents.AsNoTracking();

        if (query.Status is not null)
            eventsQuery = eventsQuery.Where(e => e.Status == query.Status);

        if (query.Type.HasValue())
            eventsQuery = eventsQuery.Where(e => e.Type == query.Type);

        if (query.InstitutionId is not null)
            eventsQuery = eventsQuery.Where(e => e.InstitutionId == query.InstitutionId);

        if (query.EntityUid.HasValue())
            eventsQuery = eventsQuery.Where(e => e.EntityUid == query.EntityUid);

        if (query.From is not null)
            eventsQuery = eventsQuery.Where(e => e.OccurredAt >= query.From);

        if (query.To is not null)
            eventsQuery = eventsQuery.Where(e => e.OccurredAt <= query.To);

        var total = await eventsQuery.CountAsync();

        var items = await eventsQuery
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new GetDomainEventsItemOut
            {
                Id = e.Id,
                InstitutionId = e.InstitutionId,
                EntityUid = e.EntityUid,
                Type = e.Type,
                Status = e.Status,
                OccurredAt = e.OccurredAt,
                ProcessedAt = e.ProcessedAt,
                Duration = e.Duration,
                Error = e.Error,
            })
            .ToListAsync();

        return new GetDomainEventsOut
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items,
        };
    }
}

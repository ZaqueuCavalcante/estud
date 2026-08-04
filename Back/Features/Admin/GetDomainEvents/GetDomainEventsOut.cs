namespace Estud.Back.Features.Admin.GetDomainEvents;

public class GetDomainEventsOut
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<GetDomainEventsItemOut> Items { get; set; } = [];
}

public class GetDomainEventsItemOut
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public string EntityUid { get; set; }
    public string Type { get; set; }
    public DomainEventStatus Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public int Duration { get; set; }
    public string? Error { get; set; }
}

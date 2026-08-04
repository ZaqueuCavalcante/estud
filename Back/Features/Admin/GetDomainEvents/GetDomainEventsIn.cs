namespace Estud.Back.Features.Admin.GetDomainEvents;

public class GetDomainEventsIn
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DomainEventStatus? Status { get; set; }
    public string? Type { get; set; }
    public int? InstitutionId { get; set; }
    public string? EntityUid { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

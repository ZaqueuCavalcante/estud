using Estud.Back.Features.Admin.GetDomainEvents;
using Estud.Back.Features.Admin.GetInstitutions;

namespace Estud.Tests.Integration.Clients;

public partial class TestsHttpClient
{
    public async Task<OneOf<GetInstitutionsOut, ErrorOut>> GetInstitutions(
        string? name = null,
        int page = 1,
        int pageSize = 20)
    {
        var data = new GetInstitutionsIn { Name = name, Page = page, PageSize = pageSize };
        var response = await http.GetAsync("/admin/institutions".AddQueryString(data));
        return await response.Resolve<GetInstitutionsOut>();
    }

    public async Task<OneOf<GetDomainEventsOut, ErrorOut>> GetDomainEvents(
        DomainEventStatus? status = null,
        int? institutionId = null,
        string? type = null,
        string? entityUid = null,
        int page = 1,
        int pageSize = 20)
    {
        var data = new GetDomainEventsIn
        {
            Status = status,
            InstitutionId = institutionId,
            Type = type,
            EntityUid = entityUid,
            Page = page,
            PageSize = pageSize,
        };
        var response = await http.GetAsync("/admin/domain-events".AddQueryString(data));
        return await response.Resolve<GetDomainEventsOut>();
    }
}

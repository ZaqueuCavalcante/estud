namespace Estud.Back.Features.Admin.GetInstitutions;

public class GetInstitutionsIn
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
}

namespace Estud.Back.Features.Admin.GetInstitutions;

public class GetInstitutionsOut
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<GetInstitutionsItemOut> Items { get; set; } = [];
}

public class GetInstitutionsItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UsersCount { get; set; }
}

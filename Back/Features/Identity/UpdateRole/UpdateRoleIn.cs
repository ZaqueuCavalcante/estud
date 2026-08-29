namespace Estud.Back.Features.Identity.UpdateRole;

public class UpdateRoleIn : IApiDto<UpdateRoleIn>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<int> Permissions { get; set; } = [];

    public static IEnumerable<(string, UpdateRoleIn)> GetExamples() =>
    [
        ("Exemplo", new() { Name = "Admin", Description = "Perfil de administrador", Permissions = [1, 2, 3] }),
    ];
}

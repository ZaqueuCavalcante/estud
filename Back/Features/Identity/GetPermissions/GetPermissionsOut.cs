using Estud.Back.Auth.Permissions;

namespace Estud.Back.Features.Identity.GetPermissions;

public class GetPermissionsOut : IApiDto<GetPermissionsOut>
{
    public List<GetPermissionsItemOut> Items { get; set; } = [];

    public static IEnumerable<(string, GetPermissionsOut)> GetExamples() =>
    [
        ("Exemplo", new()
        {
            Items =
            [
                new()
                {
                    Id = 0,
                    Name = "Gerenciar perfis de acesso.",
                    Description = "Criar, editar e deletar perfis de acesso.",
                    Group = PermissionGroup.Identity,
                    AllowedTypes = [UserType.Manager],
                },
                new()
                {
                    Id = 100,
                    Name = "Gerenciar configurações da instituição.",
                    Description = "Configurar nota e frequência mínimas para aprovação.",
                    Group = PermissionGroup.Institutions,
                    AllowedTypes = [UserType.Manager],
                },
            ],
        }),
    ];
}

public class GetPermissionsItemOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public PermissionGroup Group { get; set; }
    public List<UserType> AllowedTypes { get; set; }
}

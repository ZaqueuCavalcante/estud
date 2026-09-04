using Estud.Back.Auth.Permissions;

namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetInstitutionConfig = nameof(GetInstitutionConfig);
    public const string SetupInstitutionConfig = nameof(SetupInstitutionConfig);
    public const string GetInstitutionNoteTypes = nameof(GetInstitutionNoteTypes);

    public static AuthorizationBuilder AddInstitutionsPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetInstitutionConfig, UserType.Manager, EstudPermissions.ManageInstitutionConfig)
            .AddEstudPolicy(SetupInstitutionConfig, UserType.Manager, EstudPermissions.ManageInstitutionConfig);

        builder
            .AddEstudPolicy(GetInstitutionNoteTypes, UserType.Teacher);

        return builder;
    }
}

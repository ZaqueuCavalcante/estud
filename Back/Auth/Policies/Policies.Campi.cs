using Estud.Back.Auth.Permissions;

namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetCampi = nameof(GetCampi);
    public const string GetCampus = nameof(GetCampus);
    public const string CreateCampus = nameof(CreateCampus);
    public const string UpdateCampus = nameof(UpdateCampus);
    public const string GetCampusOccupancy = nameof(GetCampusOccupancy);
    public const string GetCampusOpeningHours = nameof(GetCampusOpeningHours);
    public const string UpdateCampusOpeningHours = nameof(UpdateCampusOpeningHours);

    public static AuthorizationBuilder AddCampiPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetCampi, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(GetCampus, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(CreateCampus, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(UpdateCampus, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(GetCampusOccupancy, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(GetCampusOpeningHours, UserType.Manager, EstudPermissions.ManageCampi)
            .AddEstudPolicy(UpdateCampusOpeningHours, UserType.Manager, EstudPermissions.ManageCampi);

        return builder;
    }
}

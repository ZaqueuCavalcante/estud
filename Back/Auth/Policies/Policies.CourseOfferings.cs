using Estud.Back.Auth.Permissions;

namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetCourseOfferings = nameof(GetCourseOfferings);
    public const string CreateCourseOffering = nameof(CreateCourseOffering);
    public const string GetCourseOfferingDetails = nameof(GetCourseOfferingDetails);

    public static AuthorizationBuilder AddCourseOfferingsPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetCourseOfferings, UserType.Manager, EstudPermissions.ManageCourseOfferings)
            .AddEstudPolicy(CreateCourseOffering, UserType.Manager, EstudPermissions.ManageCourseOfferings)
            .AddEstudPolicy(GetCourseOfferingDetails, UserType.Manager, EstudPermissions.ManageCourseOfferings);

        return builder;
    }
}

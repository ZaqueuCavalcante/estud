namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetAttendance = nameof(GetAttendance);
    public const string GetPendingAttendance = nameof(GetPendingAttendance);
    public const string GetLowestAttendanceClasses = nameof(GetLowestAttendanceClasses);

    public static AuthorizationBuilder AddInsightsPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetAttendance, UserType.Manager)
            .AddEstudPolicy(GetPendingAttendance, UserType.Manager)
            .AddEstudPolicy(GetLowestAttendanceClasses, UserType.Manager);

        return builder;
    }
}

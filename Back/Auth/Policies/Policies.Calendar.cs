using Estud.Back.Auth.Permissions;

namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetCalendar = nameof(GetCalendar);
    public const string CreateCalendarDay = nameof(CreateCalendarDay);
    public const string UpdateCalendarDay = nameof(UpdateCalendarDay);
    public const string DeleteCalendarDay = nameof(DeleteCalendarDay);

    public static AuthorizationBuilder AddCalendarPolicies(this AuthorizationBuilder builder)
    {
        return builder
            .AddEstudPolicy(GetCalendar, UserType.Manager, EstudPermissions.ManageCalendar)
            .AddEstudPolicy(CreateCalendarDay, UserType.Manager, EstudPermissions.ManageCalendar)
            .AddEstudPolicy(UpdateCalendarDay, UserType.Manager, EstudPermissions.ManageCalendar)
            .AddEstudPolicy(DeleteCalendarDay, UserType.Manager, EstudPermissions.ManageCalendar);
    }
}

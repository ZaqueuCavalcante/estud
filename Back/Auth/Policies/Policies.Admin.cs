namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetDomainEvents = nameof(GetDomainEvents);
    public const string GetInstitutions = nameof(GetInstitutions);

    public static AuthorizationBuilder AddAdminPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudAdminPolicy(GetDomainEvents)
            .AddEstudAdminPolicy(GetInstitutions);

        return builder;
    }
}

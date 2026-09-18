namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetAuthStatus = nameof(GetAuthStatus);
    public const string GetUserAccount = nameof(GetUserAccount);
    public const string UpdateUserAccount = nameof(UpdateUserAccount);

    public const string UpdateProfilePhoto = nameof(UpdateProfilePhoto);
    public const string RemoveProfilePhoto = nameof(RemoveProfilePhoto);
    public const string CreateProfilePhotoUpload = nameof(CreateProfilePhotoUpload);

    public static AuthorizationBuilder AddUsersPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetAuthStatus)
            .AddEstudPolicy(GetUserAccount)
            .AddEstudPolicy(UpdateUserAccount);

        builder
            .AddEstudPolicy(UpdateProfilePhoto)
            .AddEstudPolicy(RemoveProfilePhoto)
            .AddEstudPolicy(CreateProfilePhotoUpload);

        return builder;
    }
}

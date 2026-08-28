using Estud.Back.Auth.Permissions;

namespace Estud.Back.Auth.Policies;

public static partial class Policies
{
    public const string GetCourseCurriculums = nameof(GetCourseCurriculums);
    public const string CreateCourseCurriculum = nameof(CreateCourseCurriculum);
    public const string UpdateCourseCurriculum = nameof(UpdateCourseCurriculum);
    public const string GetCourseCurriculumDetails = nameof(GetCourseCurriculumDetails);

    public static AuthorizationBuilder AddCourseCurriculumsPolicies(this AuthorizationBuilder builder)
    {
        builder
            .AddEstudPolicy(GetCourseCurriculums, UserType.Manager, EstudPermissions.ManageCourseCurriculums)
            .AddEstudPolicy(CreateCourseCurriculum, UserType.Manager, EstudPermissions.ManageCourseCurriculums)
            .AddEstudPolicy(UpdateCourseCurriculum, UserType.Manager, EstudPermissions.ManageCourseCurriculums)
            .AddEstudPolicy(GetCourseCurriculumDetails, UserType.Manager, EstudPermissions.ManageCourseCurriculums);

        return builder;
    }
}

namespace Estud.Back.Configs;

public static class AuthorizationConfigs
{
    public static void AddAuthorizationConfigs(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorizationBuilder()
            .AddAdminPolicies()
            .AddUsersPolicies()
            .AddCampiPolicies()
            .AddCoursesPolicies()
            .AddClassesPolicies()
            .AddCalendarPolicies()
            .AddIdentityPolicies()
            .AddTeachersPolicies()
            .AddStudentsPolicies()
            .AddWebhooksPolicies()
            .AddInsightsPolicies()
            .AddClassroomsPolicies()
            .AddDisciplinesPolicies()
            .AddInstitutionsPolicies()
            .AddNotificationsPolicies()
            .AddAcademicPeriodsPolicies()
            .AddCourseOfferingsPolicies()
            .AddCourseCurriculumsPolicies();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            };
        });
    }
}

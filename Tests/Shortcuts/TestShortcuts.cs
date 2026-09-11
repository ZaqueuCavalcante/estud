using Estud.Tests.Integration.Clients;
using Estud.Back.Features.Identity.VerifySsoDomain;
using Estud.Back.Features.Periods.GetAcademicPeriods;
using Estud.Back.Features.Identity.CreateSsoConfiguration;

namespace Estud.Tests.Shortcuts;

public static class TestShortcuts
{
    public static async Task<CreateSsoConfigurationOut> ShortcutCreateVerifiedSsoConfiguration(
        this TestsHttpClient client,
        SsoProviderType providerType = SsoProviderType.AzureAd,
        string authority = "https://login.microsoftonline.com/tenant-id/v2.0",
        string clientId = "00000000-0000-0000-0000-000000000000",
        bool requireSso = false,
        string? domain = null
    ) {
        var config = await client.CreateSsoConfiguration(providerType, authority, clientId, requireSso: requireSso, domain: domain).Success();
        await client.ShortcutVerifySsoDomain(config.Id);

        return config;
    }

    public static async Task<VerifySsoDomainOut> ShortcutVerifySsoDomain(this TestsHttpClient client, Guid ssoConfigurationId)
    {
        var domain = (await client.GetSsoConfiguration().Success()).Domains.Single();
        await MocksFactory.PublishDnsTxtRecords(domain.TxtRecordName, domain.TxtRecordValue);

        return await client.VerifySsoDomain(ssoConfigurationId, domain.Domain).Success();
    }

    public static async Task<GetAcademicPeriodsItemOut> ShortcutGetFirstAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.First();
    }

    public static async Task<GetAcademicPeriodsItemOut> ShortcutGetLastAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.Last();
    }

    public static async Task<List<int>> ShortcutGetClassLessons(this TestsHttpClient client, int classId)
    {
        var lessons = await client.GetTeacherClassLessons(classId).Success();

        return lessons.Lessons.Select(x => x.Id).ToList();
    }

    public static async Task ShortcutAddStudentActivityNote(
        this TestsHttpClient client,
        int classId,
        int activityId,
        int studentId,
        decimal note
    ) {
        var activity = await client.GetTeacherClassActivity(classId, activityId).Success();
        var work = activity.Works.First(w => w.StudentId == studentId);

        await client.AddActivityNote(activityId, work.Id, note);
    }

    public static async Task<ShortcutCreateClassDto> ShortcutCreateStartedClass(
        this TestsHttpClient client,
        List<int>? students = null,
        string disciplineName = "Geometria",
        Day day = Day.Monday,
        int studentsCount = 1,
        int? periodId = null
    ) {
        var discipline = await client.CreateDiscipline(disciplineName).Success();
        var teacher = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        periodId ??= (await client.ShortcutGetFirstAcademicPeriod()).Id;
        var @class = await client.CreateClass(discipline.Id, periodId.Value).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(day, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        await client.ReleaseClassForEnrollment(@class.Id);

        var result = new ShortcutCreateClassDto { Id = @class.Id, TeacherEmail = teacher.Email };

        if (students is null)
        {
            students = [];
            for (var i = 0; i < studentsCount; i++)
            {
                var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
                students.Add(student.Id);
                if (i == 0) result.StudentEmail = student.Email;
            }
        }

        foreach (var studentId in students)
            await client.AssignStudentToClass(studentId, @class.Id);

        result.StudentIds = students;

        await client.StartClass(@class.Id);

        return result;
    }
}

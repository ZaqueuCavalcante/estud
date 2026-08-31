using Estud.Tests.Integration.Clients;
using Estud.Back.Features.Periods.GetAcademicPeriods;

namespace Estud.Tests.Shortcuts;

public static class TestShortcuts
{
    public static async Task<GetAcademicPeriodsItemOut> GetFirstAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.First();
    }

    public static async Task<GetAcademicPeriodsItemOut> GetLastAcademicPeriod(this TestsHttpClient client)
    {
        return (await client.GetAcademicPeriods().Success()).Items.Last();
    }

    public static async Task<List<int>> GetClassLessons(this TestsHttpClient client, int classId)
    {
        var lessons = await client.GetTeacherClassLessons(classId).Success();

        return lessons.Lessons.Select(x => x.Id).ToList();
    }

    public static async Task<List<int>> EnrollStudentsInClass(this TestsHttpClient client, int classId, int count)
    {
        await client.ReleaseClassForEnrollment(classId);

        List<int> students = [];
        for (var i = 0; i < count; i++)
        {
            var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
            await client.AssignStudentToClass(student.Id, classId);
            students.Add(student.Id);
        }

        return students;
    }
}

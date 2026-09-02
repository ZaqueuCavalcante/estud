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

        periodId ??= (await client.GetFirstAcademicPeriod()).Id;
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

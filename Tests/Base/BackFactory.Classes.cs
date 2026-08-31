using Estud.Tests.Integration.Clients;

namespace Estud.Tests.Base;

public record StartedClass(int ClassId, TestsHttpClient Teacher);

public static class BackFactoryClasses
{
    public static async Task<StartedClass> ArrangeStartedClass(
        this BackFactory factory,
        TestsHttpClient director,
        int periodId,
        List<int> students,
        string disciplineName = "Geometria",
        Day day = Day.Monday
    ) {
        var teacherEmail = DataGen.Email;
        var teacher = await director.CreateTeacher(DataGen.UserName, teacherEmail).Success();

        var discipline = await director.CreateDiscipline(disciplineName).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, periodId).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id, [(day, Hour.H07_00, Hour.H10_00, null, null)]);
        await director.ReleaseClassForEnrollment(@class.Id);

        foreach (var studentId in students)
            await director.AssignStudentToClass(studentId, @class.Id);

        await director.StartClass(@class.Id);

        return new StartedClass(@class.Id, await factory.LoginAs(teacherEmail));
    }
}

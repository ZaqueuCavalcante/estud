namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_pending_activities_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_pending_activities_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_pending_activities_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_get_zero_when_class_has_no_activities()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(0);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_get_pending_activities_of_all_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometriaClass = await director.ShortcutCreateStartedClass([student.Id], "Geometria");
        var algebraClass = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometriaTeacher = await _back.LoginAs(geometriaClass.TeacherEmail);
        await geometriaTeacher.CreateClassActivity(geometriaClass.Id, ClassNoteType.N1, weight: 50);
        await geometriaTeacher.CreateClassActivity(geometriaClass.Id, ClassNoteType.N2, weight: 50);

        var algebraTeacher = await _back.LoginAs(algebraClass.TeacherEmail);
        await algebraTeacher.CreateClassActivity(algebraClass.Id, ClassNoteType.N1, weight: 100);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(3);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_activities_already_delivered()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var delivered = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50);

        var client = await _back.LoginAs(@class.StudentEmail);
        await client.CreateClassActivityWork(delivered.Id);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(1);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_exams()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        await teacher.CreateClassActivity(@class.Id, type: ClassActivityType.Exam, weight: 50);
        await teacher.CreateClassActivity(@class.Id, type: ClassActivityType.Work, weight: 50);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(1);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_activities_of_finalized_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        await teacher.CreateClassActivity(@class.Id);

        await director.FinalizeClass(@class.Id);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(0);
    }

    [Test]
    public async Task Students_GetStudentPendingActivities_Should_not_get_activities_of_another_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        await teacher.CreateClassActivity(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentPendingActivities();

        // Assert
        result.Success.Total.Should().Be(0);
    }

    #endregion
}

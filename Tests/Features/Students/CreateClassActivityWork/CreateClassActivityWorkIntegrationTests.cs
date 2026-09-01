namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateClassActivityWork(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateClassActivityWork(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivityWork(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [TestCase("")]
    [TestCase(null)]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_with_invalid_link(string? link)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWork(activityId: 1, link);

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkLink.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_activity_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWork(activityId: 999999);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWork(activity.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWork_Should_not_create_work_when_student_enrolled_after_activity_creation()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWork(activity.Id);

        // Assert
        result.ShouldBeError(ClassActivityWorkNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_CreateClassActivityWork_Should_create_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Assert
        var work = result.Success;
        work.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task Students_CreateClassActivityWork_Should_update_link_when_work_is_delivered_again()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        await client.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Act
        var result = await client.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud/pulls");

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion
}

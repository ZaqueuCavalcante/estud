namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AddActivityNote(activityId: 1, workId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.AddActivityNote(activityId: 1, workId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.AddActivityNote(activityId: 1, workId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_when_activity_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AddActivityNote(activityId: 999999, workId: 1);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_on_activity_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id).Success();
        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_on_activity_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id).Success();
        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_when_work_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        // Act
        var result = await client.AddActivityNote(activity.Id, workId: 999999);

        // Assert
        result.ShouldBeError(ClassActivityWorkNotFound.I);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_not_add_note_on_work_of_another_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);

        var activityA = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var activityB = await client.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 40).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activityB.Id).Success()).Works[0];

        // Act
        var result = await client.AddActivityNote(activityA.Id, work.Id);

        // Assert
        result.ShouldBeError(ClassActivityWorkNotFound.I);
    }

    [TestCase(-1)]
    [TestCase(10.1)]
    public async Task Teachers_AddActivityNote_Should_not_add_invalid_note(decimal note)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id, note);

        // Assert
        result.ShouldBeError(InvalidStudentClassNote.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_AddActivityNote_Should_add_note_on_delivered_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        var student = await _back.LoginAs(@class.StudentEmail);
        await student.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id, 8.5m);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        updated.Works[0].Value.Should().Be(8.5m);
        updated.Works[0].Status.Should().Be(ClassActivityWorkStatus.Finalized);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_add_note_on_pending_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id, 0);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        updated.Works[0].Value.Should().Be(0);
        updated.Works[0].Status.Should().Be(ClassActivityWorkStatus.Finalized);
    }

    [Test]
    public async Task Teachers_AddActivityNote_Should_update_note_when_added_again()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var work = (await client.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];

        await client.AddActivityNote(activity.Id, work.Id, 5);

        // Act
        var result = await client.AddActivityNote(activity.Id, work.Id, 10);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        updated.Works[0].Value.Should().Be(10);
    }

    #endregion
}

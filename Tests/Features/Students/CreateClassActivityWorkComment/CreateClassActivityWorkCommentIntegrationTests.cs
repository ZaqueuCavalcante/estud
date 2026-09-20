namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [TestCase("")]
    [TestCase(null)]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_with_invalid_content(string? content)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 1, content);

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkContent.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_with_content_too_long()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 1, new string('a', 10_001));

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkContent.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_activity_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkComment(activityId: 999999);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_activity_is_an_exam()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, type: ClassActivityType.Exam, weight: 40).Success();
        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await student.CreateClassActivityWorkComment(activity.Id);

        // Assert
        result.ShouldBeError(ClassActivityDoesNotAcceptWorks.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_not_create_comment_when_work_is_finalized()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        await client.CreateClassActivityWorkComment(activity.Id, "Segue a modelagem.");

        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        await teacher.CreateClassActivityWorkEntry(activity.Id, work.Id, note: 8.5m, status: ClassActivityWorkStatus.Finalized);

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id, "Professor, posso refazer?");

        // Assert
        result.ShouldBeError(ClassActivityWorkAlreadyFinalized.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_create_comment()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Assert
        var work = result.Success;
        work.Id.Should().BeGreaterThan(0);

        var classActivity = await client.GetStudentClassActivity(@class.Id, activity.Id).Success();
        classActivity.WorkStatus.Should().Be(ClassActivityWorkStatus.Review);
        classActivity.WorkEntries.Should().ContainSingle(e =>
            e.Type == ClassActivityWorkEntryType.Comment &&
            e.Content == "https://github.com/ZaqueuCavalcante/estud" &&
            e.Metadata == null
        );
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_create_comment_when_student_enrolled_after_activity_creation()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var studentName = DataGen.UserName;
        var student = await director.CreateStudent(studentName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);
        var client = await _back.LoginAs(student.Email);

        var content = "Segue a modelagem.\n\n![Diagrama](https://estud.storage.com/class-activity-work-files/diagrama.png)";

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id, content);

        // Assert
        var work = result.Success;
        work.Id.Should().BeGreaterThan(0);

        var classActivity = await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        var studentWork = classActivity.Works.Single(w => w.StudentId == student.Id);
        studentWork.Status.Should().Be(ClassActivityWorkStatus.Review);
        studentWork.Entries.Should().ContainSingle(e =>
            e.Type == ClassActivityWorkEntryType.Comment &&
            e.User == studentName &&
            e.Content == content
        );
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_keep_previous_comments_on_timeline()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        await client.CreateClassActivityWorkComment(activity.Id, "Primeira versão");

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id, "Segunda versão");

        // Assert
        result.ShouldBeSuccess();

        var classActivity = await client.GetStudentClassActivity(@class.Id, activity.Id).Success();
        classActivity.WorkEntries.Should().HaveCount(2);
        classActivity.WorkEntries[0].Content.Should().Be("Primeira versão");
        classActivity.WorkEntries[1].Content.Should().Be("Segunda versão");
    }

    [Test]
    public async Task Students_CreateClassActivityWorkComment_Should_create_comment_after_teacher_comment()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        var work = (await teacher.GetTeacherClassActivity(@class.Id, activity.Id).Success()).Works[0];
        await teacher.CreateClassActivityWorkEntry(activity.Id, work.Id, content: "Lembra que o prazo é sexta.");

        // Act
        var result = await client.CreateClassActivityWorkComment(activity.Id, "Obrigada, professor!");

        // Assert
        result.ShouldBeSuccess();

        var classActivity = await client.GetStudentClassActivity(@class.Id, activity.Id).Success();
        classActivity.WorkStatus.Should().Be(ClassActivityWorkStatus.Review);
        classActivity.WorkEntries.Should().HaveCount(2);
        classActivity.WorkEntries[0].Content.Should().Be("Lembra que o prazo é sexta.");
        classActivity.WorkEntries[1].Content.Should().Be("Obrigada, professor!");
    }

    #endregion
}

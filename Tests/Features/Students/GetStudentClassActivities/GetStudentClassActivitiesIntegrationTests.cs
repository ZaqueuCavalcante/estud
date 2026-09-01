namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentClassActivities(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentClassActivities(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentClassActivities(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassActivities(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_of_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var otherDirector = await _back.LoggedAsDirector();
        var otherClass = await otherDirector.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(otherClass.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_not_get_activities_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_empty_list_when_class_has_no_activities()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        result.Success.Activities.Should().BeEmpty();
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_only_activities_of_the_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var class1 = await director.ShortcutCreateStartedClass();
        var class2 = await director.ShortcutCreateStartedClass();

        var teacher1Client = await _back.LoginAs(class1.TeacherEmail);
        await teacher1Client.CreateClassActivity(class1.Id, ClassNoteType.N1, type: ClassActivityType.Work, weight: 25);
        await teacher1Client.CreateClassActivity(class1.Id, ClassNoteType.N2, type: ClassActivityType.Presentation, weight: 10);
        await teacher1Client.CreateClassActivity(class1.Id, ClassNoteType.N2, type: ClassActivityType.Work, weight: 30);

        var teacher2Client = await _back.LoginAs(class2.TeacherEmail);
        await teacher2Client.CreateClassActivity(class2.Id, ClassNoteType.N1, type: ClassActivityType.Work, weight: 80);

        var client = await _back.LoginAs(class1.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(class1.Id);

        // Assert
        var activities = result.Success.Activities;
        activities.Should().HaveCount(3);
        activities.Select(a => a.Note).Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N2);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_activity_with_pending_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var dueDate = DateTime.UtcNow.AddDays(7).ToDateOnly();
        await teacherClient.CreateClassActivity(
            @class.Id,
            ClassNoteType.N2,
            "Modelagem de Banco de Dados",
            "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
            ClassActivityType.Work,
            40,
            dueDate,
            Hour.H08_30
        );

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        var activity = result.Success.Activities.Should().ContainSingle().Subject;
        activity.ClassId.Should().Be(@class.Id);
        activity.Note.Should().Be(ClassNoteType.N2);
        activity.Title.Should().Be("Modelagem de Banco de Dados");
        activity.Description.Should().Be("Modele um banco de dados para um sistema de gerenciamento de biblioteca.");
        activity.Type.Should().Be(ClassActivityType.Work);
        activity.Status.Should().Be(ClassActivityStatus.Pending);
        activity.Weight.Should().Be(40);
        activity.DueDate.Should().Be(dueDate);
        activity.DueHour.Should().Be(Hour.H08_30);
        activity.WorkStatus.Should().Be(ClassActivityWorkStatus.Pending);
        activity.WorkLink.Should().BeNull();
        activity.Value.Should().Be(0);
        activity.PonderedValue.Should().Be(0);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_activity_with_delivered_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id, weight: 40).Success();

        var client = await _back.LoginAs(@class.StudentEmail);
        await client.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        var item = result.Success.Activities.Should().ContainSingle().Subject;
        item.Id.Should().Be(activity.Id);
        item.WorkStatus.Should().Be(ClassActivityWorkStatus.Delivered);
        item.WorkLink.Should().Be("https://github.com/ZaqueuCavalcante/estud");
    }

    #endregion
}

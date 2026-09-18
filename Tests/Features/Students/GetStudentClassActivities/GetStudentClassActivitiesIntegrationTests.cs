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

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_activity_with_finalized_work()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id, weight: 40).Success();

        var client = await _back.LoginAs(@class.StudentEmail);
        await client.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, activity.Id, @class.StudentIds[0], 8.5m);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        var item = result.Success.Activities.Should().ContainSingle().Subject;
        item.Id.Should().Be(activity.Id);
        item.Weight.Should().Be(40);
        item.WorkStatus.Should().Be(ClassActivityWorkStatus.Finalized);
        item.WorkLink.Should().Be("https://github.com/ZaqueuCavalcante/estud");
        item.Value.Should().Be(8.5m);
        item.PonderedValue.Should().Be(3.4m);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_activities_ordered_by_note_and_creation()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N2, "N2 - Primeira", weight: 30);
        await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, "N1 - Primeira", weight: 30);
        await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N2, "N2 - Segunda", weight: 30);
        await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, "N1 - Segunda", weight: 30);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        result.Success.Activities.Select(a => a.Title).Should().Equal(
            "N1 - Primeira", "N1 - Segunda", "N2 - Primeira", "N2 - Segunda"
        );
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_only_the_notes_of_the_logged_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var otherStudent = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass(students: [student.Id, otherStudent.Id]);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id, weight: 20).Success();
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, activity.Id, student.Id, 9);
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, activity.Id, otherStudent.Id, 4);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert
        var item = result.Success.Activities.Should().ContainSingle().Subject;
        item.Value.Should().Be(9);
        item.PonderedValue.Should().Be(1.8m);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_every_note_type_of_the_institution_without_performance_when_there_are_no_activities()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await student.GetStudentClassActivities(@class.Id);

        // Assert
        var notes = result.Success.Notes;
        notes.Select(n => n.Note).Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3);
        notes.Should().OnlyContain(n => n.Performance == null);
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_get_performance_over_the_weight_of_the_activities_of_each_note()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var first = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var second = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, first.Id, @class.StudentIds[0], 8);
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, second.Id, @class.StudentIds[0], 6);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert — (8 × 40 + 6 × 30) / 70 = 71.428..., e os 30 de peso sem atividade ficam de fora
        result.Success.Notes.Single(n => n.Note == ClassNoteType.N1).Performance.Should().Be(71.4M);
        result.Success.Notes.Single(n => n.Note == ClassNoteType.N2).Performance.Should().BeNull();
    }

    [Test]
    public async Task Students_GetStudentClassActivities_Should_count_an_activity_without_note_as_zero_in_the_performance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var graded = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 60).Success();
        await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 40);
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, graded.Id, @class.StudentIds[0], 10);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassActivities(@class.Id);

        // Assert — (10 × 60 + 0 × 40) / 100
        result.Success.Notes.Single(n => n.Note == ClassNoteType.N2).Performance.Should().Be(60M);
    }

    [Test]
    [TestCase(25, 2.0)]
    [TestCase(100, 8.0)]
    public async Task Students_GetStudentClassActivities_Should_recalculate_the_pondered_value_when_the_weight_of_a_graded_activity_changes(int newWeight, decimal newPonderedValue)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, activity.Id, @class.StudentIds[0], 8);

        var client = await _back.LoginAs(@class.StudentEmail);
        var before = await client.GetStudentClassActivities(@class.Id).Success();

        // Act
        await teacherClient.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N1, weight: newWeight);

        // Assert
        before.Activities.Single().PonderedValue.Should().Be(4M);

        var after = await client.GetStudentClassActivities(@class.Id).Success();
        after.Activities.Single().Value.Should().Be(8M);
        after.Activities.Single().Weight.Should().Be(newWeight);
        after.Activities.Single().PonderedValue.Should().Be(newPonderedValue);
    }

    [Test]
    [TestCase(50, 25, 80.0)]
    [TestCase(25, 50, 60.0)]
    public async Task Students_GetStudentClassActivities_Should_recalculate_the_performance_when_the_weight_of_a_graded_activity_changes(int firstWeight, int secondWeight, decimal newPerformance)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var first = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        var second = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, first.Id, @class.StudentIds[0], 10);
        await teacherClient.ShortcutAddStudentActivityNote(@class.Id, second.Id, @class.StudentIds[0], 4);

        var client = await _back.LoginAs(@class.StudentEmail);
        var before = await client.GetStudentClassActivities(@class.Id).Success();

        // Act
        await teacherClient.UpdateClassActivity(@class.Id, first.Id, ClassNoteType.N1, weight: firstWeight);
        await teacherClient.UpdateClassActivity(@class.Id, second.Id, ClassNoteType.N1, weight: secondWeight);

        // Assert — (10 × 50 + 4 × 50) / 100 = 70 antes; depois (10 × 50 + 4 × 25) / 75 = 80 ou (10 × 25 + 4 × 50) / 75 = 60
        before.Notes.Single(n => n.Note == ClassNoteType.N1).Performance.Should().Be(70M);

        var after = await client.GetStudentClassActivities(@class.Id).Success();
        after.Notes.Single(n => n.Note == ClassNoteType.N1).Performance.Should().Be(newPerformance);
    }

    #endregion
}

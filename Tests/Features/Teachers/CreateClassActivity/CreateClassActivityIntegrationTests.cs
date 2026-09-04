namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateClassActivity(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateClassActivity(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivity(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_on_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivity(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_on_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var teacherClient = await _back.LoginAs(teacher.Email);

        var discipline = await director.CreateDiscipline().Success();
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(otherTeacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await teacherClient.CreateClassActivity(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [TestCase(-1)]
    [TestCase(101)]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_with_invalid_weight(int weight)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.CreateClassActivity(@class.Id, weight: weight);

        // Assert
        result.ShouldBeError(InvalidClassActivityWeight.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_with_note_type_not_used_by_the_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.CreateClassActivity(@class.Id, ClassNoteType.N3);

        // Assert
        result.ShouldBeError(NoteTypeNotUsedByInstitution.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_with_unknown_note_type()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.CreateClassActivity(@class.Id, (ClassNoteType)69);

        // Assert
        result.ShouldBeError(NoteTypeNotUsedByInstitution.I);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_not_create_activity_when_note_weights_sum_exceeds_100()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 70);

        // Act
        var result = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 31);

        // Assert
        result.ShouldBeError(InvalidClassActivityWeight.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_CreateClassActivity_Should_create_class_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);
        var dueDate = DateTime.UtcNow.AddDays(7).ToDateOnly();

        // Act
        var result = await client.CreateClassActivity(
            @class.Id,
            ClassNoteType.N2,
            "Modelagem de Banco de Dados",
            "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
            ClassActivityType.Work,
            69,
            dueDate,
            Hour.H08_30
        );

        // Assert
        var activity = await client.GetTeacherClassActivity(@class.Id, result.Success.Id).Success();
        activity.ClassId.Should().Be(@class.Id);
        activity.Note.Should().Be(ClassNoteType.N2);
        activity.Title.Should().Be("Modelagem de Banco de Dados");
        activity.Description.Should().Be("Modele um banco de dados para um sistema de gerenciamento de biblioteca.");
        activity.Type.Should().Be(ClassActivityType.Work);
        activity.Status.Should().Be(ClassActivityStatus.Pending);
        activity.Weight.Should().Be(69);
        activity.DueDate.Should().Be(dueDate);
        activity.DueHour.Should().Be(Hour.H08_30);
        activity.DeliveredWorks.Should().Be(0);
        activity.TotalWorks.Should().Be(0);
        activity.Works.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_create_activities_with_valid_weights_on_each_note()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        await client.CreateClassActivity(@class.Id, ClassNoteType.N1, type: ClassActivityType.Work, weight: 25);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N1, type: ClassActivityType.Exam, weight: 75);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N2, type: ClassActivityType.Presentation, weight: 40);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N2, type: ClassActivityType.Exam, weight: 60);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N3, type: ClassActivityType.Project, weight: 100);

        // Assert
        var activities = await client.GetTeacherClassActivities(@class.Id).Success();
        activities.Activities.Should().HaveCount(5);
    }

    [TestCase(ClassNoteType.N1)]
    [TestCase(ClassNoteType.N2)]
    public async Task Teachers_CreateClassActivity_Should_create_activity_with_the_note_types_used_by_the_institution(ClassNoteType note)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.CreateClassActivity(@class.Id, note);

        // Assert
        var activity = await client.GetTeacherClassActivity(@class.Id, result.Success.Id).Success();
        activity.Note.Should().Be(note);
    }

    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public async Task Teachers_CreateClassActivity_Should_create_activity_with_the_third_note_when_the_institution_uses_it(ClassGradeRule rule)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.SetupInstitutionConfig(gradeRule: rule);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.CreateClassActivity(@class.Id, ClassNoteType.N3);

        // Assert
        var activity = await client.GetTeacherClassActivity(@class.Id, result.Success.Id).Success();
        activity.Note.Should().Be(ClassNoteType.N3);
    }

    [Test]
    public async Task Teachers_CreateClassActivity_Should_notify_class_students_through_domain_event()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var disciplineName = $"Modelagem de Dados {DataGen.Numbers}";
        var discipline = await director.CreateDiscipline(disciplineName).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.ReleaseClassForEnrollment(@class.Id);

        var studentEmails = new List<string> { DataGen.Email, DataGen.Email, DataGen.Email };
        foreach (var studentEmail in studentEmails)
        {
            var student = await director.CreateStudent(DataGen.UserName, studentEmail).Success();
            await director.AssignStudentToClass(student.Id, @class.Id);
        }

        var client = await _back.LoginAs(teacher.Email);
        var title = $"Modelagem de Banco de Dados {DataGen.Numbers}";

        // Act
        var result = await client.CreateClassActivity(@class.Id, title: title);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        foreach (var studentEmail in studentEmails)
        {
            var studentClient = await _back.LoginAs(studentEmail);
            var notifications = await studentClient.GetNotifications().Success();
            notifications.Items.Should().ContainSingle(x => x.Title == "Nova atividade");
        }
    }

    #endregion
}

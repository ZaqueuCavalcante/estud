using Newtonsoft.Json.Linq;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateClassActivity(classId: 1, activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateClassActivity(classId: 1, activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.UpdateClassActivity(classId: 1, activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateClassActivity(classId: 999999, activityId: 1);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_on_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id).Success();

        var otherTeacher = await _back.LoggedAsTeacher();

        // Act
        var result = await otherTeacher.UpdateClassActivity(@class.Id, activity.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_on_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id).Success();

        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_activity_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activityId: 999999);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_of_another_class_of_the_same_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        var otherClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassTeachers(otherClass.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        // Act
        var result = await client.UpdateClassActivity(otherClass.Id, activity.Id);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [TestCase(-1)]
    [TestCase(101)]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_with_invalid_weight(int weight)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, weight: weight);

        // Assert
        result.ShouldBeError(InvalidClassActivityWeight.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_with_note_type_not_used_by_the_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N3);

        // Assert
        result.ShouldBeError(NoteTypeNotUsedByInstitution.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_update_activity_when_note_weights_sum_exceeds_100()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 70);
        var activity = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N1, weight: 31);

        // Assert
        result.ShouldBeError(InvalidClassActivityWeight.I);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_move_activity_to_a_note_whose_weights_sum_would_exceed_100()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 80);
        var activity = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N2, weight: 30);

        // Assert
        result.ShouldBeError(InvalidClassActivityWeight.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_update_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var created = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, type: ClassActivityType.Work, weight: 40).Success();
        var dueDate = DateTime.UtcNow.AddDays(14).ToDateOnly();

        // Act
        var result = await client.UpdateClassActivity(
            @class.Id,
            created.Id,
            ClassNoteType.N2,
            "Apresentação de Normalização",
            "Apresente as **formas normais** com exemplos.",
            ClassActivityType.Presentation,
            60,
            dueDate,
            Hour.H10_00
        );

        // Assert
        result.ShouldBeSuccess();

        var activity = await client.GetTeacherClassActivity(@class.Id, created.Id).Success();
        activity.Note.Should().Be(ClassNoteType.N2);
        activity.Title.Should().Be("Apresentação de Normalização");
        activity.Description.Should().Be("Apresente as **formas normais** com exemplos.");
        activity.Type.Should().Be(ClassActivityType.Presentation);
        activity.Weight.Should().Be(60);
        activity.DueDate.Should().Be(dueDate);
        activity.DueHour.Should().Be(Hour.H10_00);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_count_the_activity_own_weight_on_the_note_sum()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 60);
        var activity = await client.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 30).Success();

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, ClassNoteType.N1, weight: 40);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        updated.Weight.Should().Be(40);
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_keep_the_students_works()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var client = await _back.LoginAs(teacher.Email);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        var studentClient = await _back.LoginAs(student.Email);
        await studentClient.CreateClassActivityWork(activity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Act
        await client.UpdateClassActivity(@class.Id, activity.Id, title: "Novo título");

        // Assert
        var updated = await client.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        updated.Title.Should().Be("Novo título");
        updated.DeliveredWorks.Should().Be(1);
        updated.Works.Should().ContainSingle();
        updated.Works[0].Link.Should().Be("https://github.com/ZaqueuCavalcante/estud");
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_notify_the_students_when_the_teacher_chooses_to()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var disciplineName = $"Modelagem de Dados {DataGen.Numbers}";
        var discipline = await director.CreateDiscipline(disciplineName).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.ReleaseClassForEnrollment(@class.Id);

        var studentEmails = new List<string> { DataGen.Email, DataGen.Email };
        foreach (var studentEmail in studentEmails)
        {
            var student = await director.CreateStudent(DataGen.UserName, studentEmail).Success();
            await director.AssignStudentToClass(student.Id, @class.Id);
        }

        var client = await _back.LoginAs(teacher.Email);
        var activity = await client.CreateClassActivity(@class.Id).Success();
        var title = $"Modelagem de Banco de Dados {DataGen.Numbers}";

        // Act
        var result = await client.UpdateClassActivity(@class.Id, activity.Id, title: title, notifyStudents: true);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        result.ShouldBeSuccess();

        foreach (var studentEmail in studentEmails)
        {
            var studentClient = await _back.LoginAs(studentEmail);
            var notifications = await studentClient.GetNotifications().Success();
            var notification = notifications.Items.Should().ContainSingle(x => x.Title == "Atividade alterada").Subject;

            notification.NotificationType.Should().Be(NotificationType.UpdatedClassActivity);
            notification.Description.Should().Be($"{disciplineName}: {title}");

            var link = ((JObject)notification.Metadata!)["links"]![0]!;
            link.Value<string>("label").Should().Be("Ver atividade");
            link.Value<string>("to").Should().Be($"/classes/{@class.Id}/activities/{activity.Id}");
        }
    }

    [Test]
    public async Task Teachers_UpdateClassActivity_Should_not_notify_the_students_by_default()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.TeacherEmail);
        var activity = await client.CreateClassActivity(@class.Id).Success();

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        await client.UpdateClassActivity(@class.Id, activity.Id, title: "Novo título");

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var studentClient = await _back.LoginAs(@class.StudentEmail);
        var notifications = await studentClient.GetNotifications().Success();
        notifications.Items.Should().ContainSingle(x => x.Title == "Nova atividade");
        notifications.Items.Should().NotContain(x => x.Title == "Atividade alterada");
    }

    #endregion
}

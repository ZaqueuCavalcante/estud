namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherClassActivity(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherClassActivity(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassActivity(999999, 1);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_of_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassActivity(@class.Id, 1);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_of_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(otherTeacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var otherClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(otherClass.Id, [otherTeacher.Id]);

        var otherClient = await _back.LoginAs(otherTeacher.Email);
        var activity = await otherClient.CreateClassActivity(otherClass.Id).Success();

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClassActivity(otherClass.Id, activity.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_when_activity_not_found()
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
        var result = await client.GetTeacherClassActivity(@class.Id, 999999);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_not_get_activity_of_another_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var period = await director.GetFirstAcademicPeriod();

        var disciplineA = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [disciplineA.Id]);
        var classA = await director.CreateClass(disciplineA.Id, period.Id).Success();
        await director.UpdateClassTeachers(classA.Id, [teacher.Id]);

        var disciplineB = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [disciplineB.Id]);
        var classB = await director.CreateClass(disciplineB.Id, period.Id).Success();
        await director.UpdateClassTeachers(classB.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);
        var activity = await client.CreateClassActivity(classB.Id).Success();

        // Act
        var result = await client.GetTeacherClassActivity(classA.Id, activity.Id);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_get_class_activity()
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
        var created = await client.CreateClassActivity(
            @class.Id,
            ClassNoteType.N1,
            "Modelagem de Banco de Dados",
            "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
            ClassActivityType.Work,
            40,
            dueDate,
            Hour.H08_30
        ).Success();

        // Act
        var result = await client.GetTeacherClassActivity(@class.Id, created.Id);

        // Assert
        var activity = result.Success;
        activity.Id.Should().Be(created.Id);
        activity.ClassId.Should().Be(@class.Id);
        activity.Note.Should().Be(ClassNoteType.N1);
        activity.Title.Should().Be("Modelagem de Banco de Dados");
        activity.Description.Should().Be("Modele um banco de dados para um sistema de gerenciamento de biblioteca.");
        activity.Type.Should().Be(ClassActivityType.Work);
        activity.Status.Should().Be(ClassActivityStatus.Pending);
        activity.Weight.Should().Be(40);
        activity.DueDate.Should().Be(dueDate);
        activity.DueHour.Should().Be(Hour.H08_30);
        activity.DeliveredWorks.Should().Be(0);
        activity.TotalWorks.Should().Be(0);
        activity.Works.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClassActivity_Should_get_class_activity_works()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var client = await _back.LoginAs(teacher.Email);
        var classActivity = await client.CreateClassActivity(@class.Id).Success();

        var studentClient = await _back.LoginAs(student.Email);
        await studentClient.CreateClassActivityWork(classActivity.Id, "https://github.com/ZaqueuCavalcante/estud");

        // Act
        var activity = await client.GetTeacherClassActivity(@class.Id, classActivity.Id).Success();

        // Assert
        activity.DeliveredWorks.Should().Be(1);
        activity.TotalWorks.Should().Be(1);
        activity.Works.Should().ContainSingle();
        activity.Works[0].StudentId.Should().Be(student.Id);
        activity.Works[0].Link.Should().Be("https://github.com/ZaqueuCavalcante/estud");
        activity.Works[0].Status.Should().Be(ClassActivityWorkStatus.Delivered);
        activity.Works[0].Value.Should().Be(0);
    }

    #endregion
}

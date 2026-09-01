namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_of_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var discipline = await otherDirector.CreateDiscipline().Success();
        var period = await otherDirector.GetFirstAcademicPeriod();
        var @class = await otherDirector.CreateClass(discipline.Id, period.Id).Success();

        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_student_is_not_enrolled_in_it()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentClass_Should_get_class_details()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        await director.ReleaseClassForEnrollment(@class.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        await director.StartClass(@class.Id);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(@class.Id);
        details.Period.Should().Be(period.Name);
        details.Discipline.Should().Be("Geometria");
        details.Status.Should().Be(ClassStatus.Started);
        details.MyStatus.Should().Be(StudentClassStatus.Matriculado);
        details.Schedules.Should().ContainSingle();
    }

    #endregion
}

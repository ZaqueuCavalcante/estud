namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignStudentToClass(studentId: 1, classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignStudentToClass(studentId: 1, classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_student_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.AssignStudentToClass(studentId: 999999, classId: 1);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.AssignStudentToClass(student.Id, classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_student_already_enrolled()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeError(StudentAlreadyEnrolledInClass.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_class_has_no_vacancies()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, vacancies: 1).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var studentA = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentB = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(studentA.Id, @class.Id);

        // Act
        var result = await client.AssignStudentToClass(studentB.Id, @class.Id);

        // Assert
        result.ShouldBeError(NoVacanciesInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_AssignStudentToClass_Should_assign_student_to_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var studentClass = await client.GetClass(@class.Id).Success();
        studentClass.Students.Should().ContainSingle(x => x.Id == student.Id && x.Status == StudentClassStatus.Matriculado);
    }

    #endregion
}

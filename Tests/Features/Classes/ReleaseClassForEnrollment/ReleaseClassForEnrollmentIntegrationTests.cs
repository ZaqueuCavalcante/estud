namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.ReleaseClassForEnrollment(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.ReleaseClassForEnrollment(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.ReleaseClassForEnrollment(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var other = await _back.LoggedAsDirector();

        // Act
        var result = await other.ReleaseClassForEnrollment(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);

        var classData = await director.GetClass(@class.Id).Success();
        classData.Status.Should().Be(ClassStatus.OnPreEnrollment);
    }

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_when_class_is_already_on_enrollment()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id).Success();

        // Act
        var result = await client.ReleaseClassForEnrollment(@class.Id);

        // Assert
        result.ShouldBeError(ClassMustBeOnPreEnrollment.I);
    }

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_not_release_class_when_class_is_started()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await client.ReleaseClassForEnrollment(@class.Id).Success();
        await client.StartClass(@class.Id).Success();

        // Act
        var result = await client.ReleaseClassForEnrollment(@class.Id);

        // Assert
        result.ShouldBeError(ClassMustBeOnPreEnrollment.I);

        var classData = await client.GetClass(@class.Id).Success();
        classData.Status.Should().Be(ClassStatus.Started);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_ReleaseClassForEnrollment_Should_release_class_for_enrollment()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2));

        // Act
        var result = await client.ReleaseClassForEnrollment(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await client.GetClass(@class.Id).Success();
        classData.Status.Should().Be(ClassStatus.OnEnrollment);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Insights_GetAttendance_Should_not_get_attendance_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Insights_GetAttendance_Should_not_get_attendance_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Insights_GetAttendance_Should_not_get_attendance_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetAttendance(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Insights_GetAttendance_Should_not_get_attendance_when_period_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetAttendance(1234);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    [Test]
    public async Task Insights_GetAttendance_Should_not_get_attendance_when_period_is_from_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var otherPeriod = await otherDirector.ShortcutGetFirstAcademicPeriod();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetAttendance(otherPeriod.Id);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Insights_GetAttendance_Should_get_empty_attendance_when_no_attendance_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        await director.ShortcutCreateStartedClass(periodId: period.Id);

        // Act
        var result = await director.GetAttendance(period.Id);

        // Assert
        var attendance = result.Success;
        attendance.Average.Should().Be(0M);
        attendance.BelowLimitClasses.Should().Be(0);
        attendance.AboveLimitClasses.Should().Be(0);
        attendance.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Insights_GetAttendance_Should_get_daily_attendance_of_period_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var carla = await director.CreateStudent("Carla Souza", DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id], "Geometria", Day.Monday, periodId: period.Id);
        var algebra = await director.ShortcutCreateStartedClass([carla.Id], "Álgebra", Day.Monday, periodId: period.Id);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var geometryLessons = (await geometryTeacher.GetTeacherClassLessons(geometry.Id).Success()).Lessons;
        await geometryTeacher.CreateLessonAttendance(geometryLessons[0].Id, [ana.Id, bruno.Id]);
        await geometryTeacher.CreateLessonAttendance(geometryLessons[1].Id, [ana.Id]);

        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);
        var algebraLessons = (await algebraTeacher.GetTeacherClassLessons(algebra.Id).Success()).Lessons;
        await algebraTeacher.CreateLessonAttendance(algebraLessons[0].Id, []);

        // Act
        var result = await director.GetAttendance(period.Id);

        // Assert
        var attendance = result.Success;
        attendance.Average.Should().Be(60M);
        attendance.BelowLimitClasses.Should().Be(1);
        attendance.AboveLimitClasses.Should().Be(1);
        attendance.Days.Should().HaveCount(2);
        attendance.Days[0].Date.Should().Be(geometryLessons[0].Date);
        attendance.Days[0].Attendance.Should().Be(66.7M);
        attendance.Days[1].Date.Should().Be(geometryLessons[1].Date);
        attendance.Days[1].Attendance.Should().Be(50M);
    }

    #endregion
}

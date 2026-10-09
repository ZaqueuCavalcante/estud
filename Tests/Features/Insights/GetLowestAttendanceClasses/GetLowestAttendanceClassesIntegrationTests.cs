namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_not_get_classes_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetLowestAttendanceClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_not_get_classes_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetLowestAttendanceClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_not_get_classes_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetLowestAttendanceClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_not_get_classes_when_period_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetLowestAttendanceClasses(1234);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_not_get_classes_when_period_is_from_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var otherPeriod = await otherDirector.ShortcutGetFirstAcademicPeriod();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetLowestAttendanceClasses(otherPeriod.Id);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_get_empty_list_when_no_attendance_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        await director.ShortcutCreateStartedClass(periodId: period.Id);

        // Act
        var result = await director.GetLowestAttendanceClasses(period.Id);

        // Assert
        result.Success.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_get_period_classes_ordered_by_lowest_attendance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var otherPeriod = await director.ShortcutGetLastAcademicPeriod();

        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var carla = await director.CreateStudent("Carla Souza", DataGen.Email).Success();
        var davi = await director.CreateStudent("Davi Rocha", DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id], "Geometria", Day.Monday, periodId: period.Id);
        var algebra = await director.ShortcutCreateStartedClass([carla.Id], "Álgebra", Day.Tuesday, periodId: period.Id);
        var physics = await director.ShortcutCreateStartedClass([davi.Id], "Física", Day.Wednesday, periodId: period.Id);
        await director.ShortcutCreateStartedClass([ana.Id], "Química", Day.Thursday, periodId: period.Id);
        var history = await director.ShortcutCreateStartedClass([bruno.Id], "História", Day.Friday, periodId: otherPeriod.Id);

        await RecordFirstLessonAttendance(geometry, [ana.Id]);
        await RecordFirstLessonAttendance(algebra, []);
        await RecordFirstLessonAttendance(physics, [davi.Id]);
        await RecordFirstLessonAttendance(history, []);

        // Act
        var result = await director.GetLowestAttendanceClasses(period.Id);

        // Assert
        var classes = result.Success.Classes;
        classes.Select(c => c.Id).Should().Equal(algebra.Id, geometry.Id, physics.Id);
        classes.Select(c => c.Attendance).Should().Equal(0M, 50M, 100M);
        classes[0].Discipline.Should().Be("Álgebra");
        classes[1].Discipline.Should().Be("Geometria");
        classes[2].Discipline.Should().Be("Física");
    }

    [Test]
    public async Task Insights_GetLowestAttendanceClasses_Should_get_at_most_five_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        for (var i = 0; i < 6; i++)
        {
            var @class = await director.ShortcutCreateStartedClass(disciplineName: $"Disciplina {i}", periodId: period.Id);
            await RecordFirstLessonAttendance(@class, []);
        }

        // Act
        var result = await director.GetLowestAttendanceClasses(period.Id);

        // Assert
        result.Success.Classes.Should().HaveCount(5);
    }

    #endregion

    private async Task RecordFirstLessonAttendance(ShortcutCreateClassDto @class, List<int> presentStudents)
    {
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, presentStudents);
    }
}

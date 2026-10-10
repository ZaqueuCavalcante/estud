namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_get_students_at_risk_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentsAtRisk(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_get_students_at_risk_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentsAtRisk(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_get_students_at_risk_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentsAtRisk(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_get_students_at_risk_when_period_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentsAtRisk(1234);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_get_students_at_risk_when_period_is_from_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var otherPeriod = await otherDirector.ShortcutGetFirstAcademicPeriod();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentsAtRisk(otherPeriod.Id);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_get_zero_students_when_period_has_no_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        // Act
        var result = await director.GetStudentsAtRisk(period.Id);

        // Assert
        var risk = result.Success;
        risk.TotalStudents.Should().Be(0);
        risk.OnlyFrequency.Should().Be(0);
        risk.OnlyGrade.Should().Be(0);
        risk.Both.Should().Be(0);
    }

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_get_students_at_risk_by_frequency_and_by_grade()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var carla = await director.CreateStudent("Carla Souza", DataGen.Email).Success();
        var davi = await director.CreateStudent("Davi Rocha", DataGen.Email).Success();

        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id, carla.Id, davi.Id], periodId: period.Id);

        await RecordFirstLessonAttendance(@class, [ana.Id, bruno.Id]);
        await GradeN1AndN2Activities(@class, new() { [ana.Id] = 8M, [bruno.Id] = 5M, [carla.Id] = 9M, [davi.Id] = 4M });

        // Act
        var result = await director.GetStudentsAtRisk(period.Id);

        // Assert
        var risk = result.Success;
        risk.TotalStudents.Should().Be(4);
        risk.OnlyFrequency.Should().Be(1);
        risk.OnlyGrade.Should().Be(1);
        risk.Both.Should().Be(1);
    }

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_get_student_at_risk_by_both_when_risks_are_in_different_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var geometry = await director.ShortcutCreateStartedClass([student.Id], "Geometria", Day.Monday, periodId: period.Id);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday, periodId: period.Id);

        await RecordFirstLessonAttendance(geometry, []);
        await GradeN1AndN2Activities(algebra, new() { [student.Id] = 3M });

        // Act
        var result = await director.GetStudentsAtRisk(period.Id);

        // Assert
        var risk = result.Success;
        risk.TotalStudents.Should().Be(1);
        risk.OnlyFrequency.Should().Be(0);
        risk.OnlyGrade.Should().Be(0);
        risk.Both.Should().Be(1);
    }

    [Test]
    public async Task Insights_GetStudentsAtRisk_Should_not_count_students_at_risk_from_another_period()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var otherPeriod = await director.ShortcutGetLastAcademicPeriod();

        await director.ShortcutCreateStartedClass(disciplineName: "Geometria", periodId: period.Id);
        var history = await director.ShortcutCreateStartedClass(disciplineName: "História", periodId: otherPeriod.Id);

        await RecordFirstLessonAttendance(history, []);
        await GradeN1AndN2Activities(history, new() { [history.StudentIds[0]] = 2M });

        // Act
        var result = await director.GetStudentsAtRisk(period.Id);

        // Assert
        var risk = result.Success;
        risk.TotalStudents.Should().Be(1);
        risk.OnlyFrequency.Should().Be(0);
        risk.OnlyGrade.Should().Be(0);
        risk.Both.Should().Be(0);
    }

    #endregion

    private async Task GradeN1AndN2Activities(ShortcutCreateClassDto @class, Dictionary<int, decimal> notes)
    {
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();

        foreach (var (studentId, note) in notes)
        {
            await teacher.ShortcutAddStudentActivityNote(@class.Id, n1.Id, studentId, note);
            await teacher.ShortcutAddStudentActivityNote(@class.Id, n2.Id, studentId, note);
        }
    }
}

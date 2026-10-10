namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_not_get_classes_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetLowestGradeClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_not_get_classes_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetLowestGradeClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_not_get_classes_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetLowestGradeClasses(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_not_get_classes_when_period_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetLowestGradeClasses(1234);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_not_get_classes_when_period_is_from_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var otherPeriod = await otherDirector.ShortcutGetFirstAcademicPeriod();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetLowestGradeClasses(otherPeriod.Id);

        // Assert
        result.ShouldBeError(AcademicPeriodNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_get_empty_list_when_no_grade_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(periodId: period.Id);

        var teacher = await _back.LoginAs(@class.TeacherEmail);
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100);

        // Act
        var result = await director.GetLowestGradeClasses(period.Id);

        // Assert
        result.Success.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_get_period_classes_ordered_by_lowest_grade()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var otherPeriod = await director.ShortcutGetLastAcademicPeriod();

        var geometry = await director.ShortcutCreateStartedClass(disciplineName: "Geometria", periodId: period.Id);
        var algebra = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra", periodId: period.Id);
        var physics = await director.ShortcutCreateStartedClass(disciplineName: "Física", periodId: period.Id);
        await director.ShortcutCreateStartedClass(disciplineName: "Química", periodId: period.Id);
        var history = await director.ShortcutCreateStartedClass(disciplineName: "História", periodId: otherPeriod.Id);

        await GradeN1Activity(geometry, 8M);
        await GradeN1Activity(algebra, 6M);
        await GradeN1Activity(physics, 10M);
        await GradeN1Activity(history, 2M);

        // Act
        var result = await director.GetLowestGradeClasses(period.Id);

        // Assert
        var classes = result.Success.Classes;
        classes.Select(c => c.Id).Should().Equal(algebra.Id, geometry.Id, physics.Id);
        classes.Select(c => c.Average).Should().Equal(3M, 4M, 5M);
        classes.Select(c => c.Discipline).Should().Equal("Álgebra", "Geometria", "Física");
    }

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_get_same_average_grade_of_the_class_details()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2, periodId: period.Id);

        await GradeN1Activity(@class, 9M);

        // Act
        var result = await director.GetLowestGradeClasses(period.Id);

        // Assert
        var details = await director.GetClass(@class.Id).Success();
        var item = result.Success.Classes.Should().ContainSingle().Subject;
        item.Average.Should().Be(2.3M);
        item.Average.Should().Be(details.AverageGrade);
    }

    [Test]
    public async Task Insights_GetLowestGradeClasses_Should_get_at_most_five_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();

        for (var i = 0; i < 6; i++)
        {
            var @class = await director.ShortcutCreateStartedClass(disciplineName: $"Disciplina {i}", periodId: period.Id);
            await GradeN1Activity(@class, 7M);
        }

        // Act
        var result = await director.GetLowestGradeClasses(period.Id);

        // Assert
        result.Success.Classes.Should().HaveCount(5);
    }

    #endregion

    private async Task GradeN1Activity(ShortcutCreateClassDto @class, decimal note)
    {
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.ShortcutAddStudentActivityNote(@class.Id, activity.Id, @class.StudentIds[0], note);
    }
}

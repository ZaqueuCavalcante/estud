namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_not_get_current_classes_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_not_get_current_classes_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_not_get_current_classes_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_get_current_classes_ordered_by_discipline_name()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometriaClass = await director.ShortcutCreateStartedClass([student.Id], "Geometria");
        var algebraClass = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        var classes = result.Success.Classes;
        classes.Should().HaveCount(2);
        classes[0].Id.Should().Be(algebraClass.Id);
        classes[0].Name.Should().Be("Álgebra");
        classes[1].Id.Should().Be(geometriaClass.Id);
        classes[1].Name.Should().Be("Geometria");
    }

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_not_get_classes_that_are_not_started()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        result.Success.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Students_GetStudentCurrentClasses_Should_not_get_classes_of_another_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.ShortcutCreateStartedClass();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentCurrentClasses();

        // Assert
        result.Success.Classes.Should().BeEmpty();
    }

    #endregion
}

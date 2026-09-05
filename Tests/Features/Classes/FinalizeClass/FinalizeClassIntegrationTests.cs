namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.FinalizeClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.FinalizeClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.FinalizeClass(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_is_not_started()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassMustBeStarted.I);
    }

    [Test]
    public async Task Classes_FinalizeClass_Should_not_finalize_class_when_class_is_already_finalized()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var @class = await client.ShortcutCreateStartedClass(students: []);
        await client.FinalizeClass(@class.Id).Success();

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassAlreadyFinalized.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_FinalizeClass_Should_finalize_started_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var @class = await client.ShortcutCreateStartedClass(students: []);

        // Act
        var result = await client.FinalizeClass(@class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classData = await client.GetClass(@class.Id).Success();
        classData.Status.Should().Be(ClassStatus.Finalized);
    }

    #endregion
}

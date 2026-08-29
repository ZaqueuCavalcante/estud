namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateTeacher_Should_not_create_teacher_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateTeacher(DataGen.UserName, DataGen.Email);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateTeacher_Should_not_create_teacher_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateTeacher(DataGen.UserName, DataGen.Email);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_CreateTeacher_Should_not_create_teacher_when_email_already_used()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.CreateTeacher(DataGen.UserName, teacher.Email);

        // Assert
        result.ShouldBeError(EmailAlreadyUsed.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_CreateTeacher_Should_create_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateTeacher(DataGen.UserName, DataGen.Email);

        // Assert
        var teacher = result.Success;
        teacher.Id.Should().BeGreaterThan(0);
    }

    #endregion
}

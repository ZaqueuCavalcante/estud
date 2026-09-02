namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Notifications_GetInstitutionNotifications_Should_not_get_notifications_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetInstitutionNotifications();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Notifications_GetInstitutionNotifications_Should_not_get_notifications_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetInstitutionNotifications();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Notifications_GetInstitutionNotifications_Should_not_get_the_welcome_notification()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateTeacher(DataGen.UserName, DataGen.Email);
        await director.CreateNotification("Aviso importante", "Descrição do aviso.", UsersGroup.Teachers);

        // Act
        var result = await director.GetInstitutionNotifications();

        // Assert
        var notifications = result.Success;
        notifications.Total.Should().Be(1);
        notifications.Items.Should().HaveCount(1);
        notifications.Items[0].Title.Should().Be("Aviso importante");
    }

    #endregion
}

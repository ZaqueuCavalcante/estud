using Estud.Back.Features.Notifications.MarkNotificationsAsViewed;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Notifications_MarkNotificationsAsViewed_Should_not_mark_notifications_as_viewed_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.MarkNotificationsAsViewed(markAll: true);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Notifications_MarkNotificationsAsViewed_Should_not_mark_notifications_as_viewed_without_id_when_not_marking_all()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.MarkNotificationsAsViewed(markAll: false, notificationId: null);

        // Assert
        result.ShouldBeError(InvalidNotificationId.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Notifications_MarkNotificationsAsViewed_Should_mark_a_single_notification_as_viewed()
    {
        // Arrange
        var manager = await _back.LoggedAsDirector();
        var teacher = await manager.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var notification = await manager.CreateNotification(targetUsers: UsersGroup.Teachers).Success();

        var teacherClient = await _back.LoginAs(teacher.Email);

        // Act
        var result = await teacherClient.MarkNotificationsAsViewed(notificationId: notification.Id);

        // Assert
        result.ShouldBeSuccess();
        var unreadCount = await teacherClient.GetUnreadNotificationsCount().Success();
        unreadCount.Count.Should().Be(0);
    }

    [Test]
    public async Task Notifications_MarkNotificationsAsViewed_Should_mark_all_notifications_as_viewed()
    {
        // Arrange
        var manager = await _back.LoggedAsDirector();
        var teacher = await manager.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await manager.CreateNotification(targetUsers: UsersGroup.Teachers);
        await manager.CreateNotification(targetUsers: UsersGroup.Teachers);

        var teacherClient = await _back.LoginAs(teacher.Email);

        // Act
        var result = await teacherClient.MarkNotificationsAsViewed(markAll: true);

        // Assert
        result.ShouldBeSuccess();
        var unreadCount = await teacherClient.GetUnreadNotificationsCount().Success();
        unreadCount.Count.Should().Be(0);
    }

    #endregion
}

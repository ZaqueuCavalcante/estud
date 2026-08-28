namespace Estud.Tests.Integration;

public partial class IntegrationTests : IntegrationTestBase
{
    #region Validation errors

    [Test]
    [TestCase("")]
    [TestCase("invalid@")]
    [TestCase("invalidemail")]
    [TestCase("@invalid.com")]
    [TestCase("invalid@.com")]
    [TestCase("invalid email@test.com")]
    public async Task Users_RegisterUser_Should_not_create_user_with_invalid_email(string email)
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var response = await client.RegisterUser(email);

        // Assert
        response.ShouldBeError(InvalidEmail.I);
    }

    [Test]
    public async Task Users_RegisterUser_Should_not_create_user_with_already_used_email()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email).Success();

        // Act
        var response = await client.RegisterUser(user.Email);

        // Assert
        response.ShouldBeError(EmailAlreadyUsed.I);
    }

    [Test]
    public async Task Users_RegisterUser_Should_not_create_user_with_already_used_email_different_casing()
    {
        // Arrange
        var client = _back.GetTestsClient();
        var user = await client.RegisterUser(DataGen.Email.ToUpper()).Success();

        // Act
        var response = await client.RegisterUser(user.Email.ToLower());

        // Assert
        response.ShouldBeError(EmailAlreadyUsed.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Users_RegisterUser_Should_create_a_new_user()
    {
       // Arrange
        var client = _back.GetTestsClient();

        // Act
        var response = await client.RegisterUser(DataGen.Email);

        // Assert
        response.ShouldBeSuccess();
    }

    [Test]
    public async Task Users_RegisterUser_Should_create_a_welcome_notification_for_the_new_manager()
    {
        // Arrange
        var manager = await _back.LoggedAsDirector();

        // Act
        var result = await manager.GetNotifications();

        // Assert
        var notifications = result.Success;
        notifications.Total.Should().Be(1);

        var welcome = notifications.Items[0];
        welcome.NotificationType.Should().Be(NotificationType.Welcome);
        welcome.Title.Should().Be("Boas-vindas ao Estud!");
        welcome.Description.Should().Contain("Configure seu perfil");
        welcome.ViewedAt.Should().BeNull();

        var unreadCount = await manager.GetUnreadNotificationsCount().Success();
        unreadCount.Count.Should().Be(1);
    }

    #endregion
}

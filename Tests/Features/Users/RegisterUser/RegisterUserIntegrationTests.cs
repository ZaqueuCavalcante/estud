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

        var email = DataGen.Email;
        await client.RegisterUser(email);

        // Act
        var response = await client.RegisterUser(email);

        // Assert
        response.ShouldBeError(EmailAlreadyUsed.I);
    }

    [Test]
    public async Task Users_RegisterUser_Should_not_create_user_with_already_used_email_different_casing()
    {
        // Arrange
        var client = _back.GetTestsClient();

        var email = DataGen.Email.ToUpper();
        await client.RegisterUser(email);

        // Act
        var response = await client.RegisterUser(email.ToLower());

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
        var email = DataGen.Email;

        // Act
        var response = await client.RegisterUser(email);

        // Assert
        response.ShouldBeSuccess();
    }

    [Test, Ignore("Fix")]
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

        var links = welcome.Metadata!.RootElement.GetProperty("links").EnumerateArray().ToList();
        links.Select(x => x.GetProperty("to").GetString()).Should().Equal("/docs", "/configs", "/account");
        links.Select(x => x.GetProperty("label").GetString()).Should()
            .Equal("Primeiros passos", "Configurar instituição", "Completar perfil");

        var unreadCount = await manager.GetUnreadNotificationsCount().Success();
        unreadCount.Count.Should().Be(1);
    }

    [Test]
    public async Task Users_RegisterUser_Should_not_send_the_welcome_notification_to_other_institution_users()
    {
        // Arrange
        var manager = await _back.LoggedAsDirector();
        var teacherEmail = DataGen.Email;
        await manager.CreateTeacher(DataGen.UserName, teacherEmail);

        var teacher = await _back.LoginAs(teacherEmail);

        // Act
        var result = await teacher.GetNotifications();

        // Assert
        var notifications = result.Success;
        notifications.Total.Should().Be(0);
        notifications.Items.Should().BeEmpty();
    }

    #endregion
}

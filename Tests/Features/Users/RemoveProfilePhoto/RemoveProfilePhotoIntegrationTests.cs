namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Users_RemoveProfilePhoto_Should_not_remove_photo_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.RemoveProfilePhoto();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Users_RemoveProfilePhoto_Should_remove_photo()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();

        var upload = await client.ShortcutUploadProfilePhoto(storage);
        await client.UpdateProfilePhoto(upload.Path).Success();

        // Act
        var result = await client.RemoveProfilePhoto();

        // Assert
        result.ShouldBeSuccess();
        (await storage.GetMetadata(StorageContainer.ProfilePhotos, upload.Path)).Should().BeNull();

        var account = await client.GetUserAccount().Success();
        account.ProfilePhoto.Should().BeNull();
    }

    [Test]
    public async Task Users_RemoveProfilePhoto_Should_succeed_when_user_has_no_photo()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.RemoveProfilePhoto();

        // Assert
        result.ShouldBeSuccess();

        var account = await client.GetUserAccount().Success();
        account.ProfilePhoto.Should().BeNull();
    }

    #endregion
}

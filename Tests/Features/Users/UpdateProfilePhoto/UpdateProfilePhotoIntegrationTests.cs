namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateProfilePhoto("1/1/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png");

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("01K5B4Z8Q2M3N4P5R6S7T8V9W0.png")]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_path_is_invalid(string? path)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateProfilePhoto(path);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoPath.I);
    }

    [Test]
    [TestCase("../../lesson-plan-images/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png")]
    [TestCase("01K5B4Z8Q2M3N4P5R6S7T8V9W0.gif")]
    [TestCase("foto.png")]
    [TestCase("a/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png")]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_file_name_is_invalid(string fileName)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var account = await client.GetUserAccount().Success();

        // Act
        var result = await client.UpdateProfilePhoto($"{account.InstitutionId}/{account.Id}/{fileName}");

        // Assert
        result.ShouldBeError(InvalidProfilePhotoPath.I);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_with_path_of_another_user()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();

        var owner = await _back.LoggedAsDirector();
        var upload = await owner.ShortcutUploadProfilePhoto(storage);

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoPath.I);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_file_was_not_uploaded()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var upload = await client.CreateProfilePhotoUpload().Success();

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeError(ProfilePhotoNotFound.I);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_uploaded_file_has_invalid_content_type()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();

        var upload = await client.CreateProfilePhotoUpload().Success();
        storage.SimulateUpload(StorageContainer.ProfilePhotos, upload.Path, "image/gif", 245_000);

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoContentType.I);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_not_update_photo_when_uploaded_file_is_too_big()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();

        var upload = await client.CreateProfilePhotoUpload().Success();
        storage.SimulateUpload(StorageContainer.ProfilePhotos, upload.Path, "image/png", 3 * 1024 * 1024 + 1);

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoSize.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_update_photo()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();
        var upload = await client.ShortcutUploadProfilePhoto(storage);

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeSuccess();
        result.Success.ProfilePhoto.Should().EndWith($"/profile-photos/{upload.Path}");

        var account = await client.GetUserAccount().Success();
        account.ProfilePhoto.Should().Be(result.Success.ProfilePhoto);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_delete_previous_photo_when_replacing_it()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();

        var first = await client.ShortcutUploadProfilePhoto(storage);
        await client.UpdateProfilePhoto(first.Path).Success();

        var second = await client.ShortcutUploadProfilePhoto(storage, "image/jpeg");

        // Act
        var result = await client.UpdateProfilePhoto(second.Path);

        // Assert
        result.ShouldBeSuccess();

        (await storage.GetMetadata(StorageContainer.ProfilePhotos, first.Path)).Should().BeNull();
        (await storage.GetMetadata(StorageContainer.ProfilePhotos, second.Path)).Should().NotBeNull();

        var account = await client.GetUserAccount().Success();
        account.ProfilePhoto.Should().EndWith(second.Path);
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_keep_file_when_confirming_the_same_photo_twice()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var client = await _back.LoggedAsDirector();

        var upload = await client.ShortcutUploadProfilePhoto(storage);
        await client.UpdateProfilePhoto(upload.Path).Success();

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeSuccess();
        (await storage.GetMetadata(StorageContainer.ProfilePhotos, upload.Path)).Should().NotBeNull();
    }

    [Test]
    public async Task Users_UpdateProfilePhoto_Should_update_photo_of_student()
    {
        // Arrange
        var storage = _back.GetFakeStorageService();
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);
        var upload = await client.ShortcutUploadProfilePhoto(storage);

        // Act
        var result = await client.UpdateProfilePhoto(upload.Path);

        // Assert
        result.ShouldBeSuccess();
    }

    #endregion
}

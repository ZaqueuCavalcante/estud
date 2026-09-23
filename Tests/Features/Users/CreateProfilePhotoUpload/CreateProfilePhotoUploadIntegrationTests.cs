namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Users_CreateProfilePhotoUpload_Should_not_create_upload_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateProfilePhotoUpload();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("image/gif")]
    [TestCase("image/svg+xml")]
    [TestCase("application/pdf")]
    public async Task Users_CreateProfilePhotoUpload_Should_not_create_upload_when_content_type_is_invalid(string? contentType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateProfilePhotoUpload(contentType: contentType!);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoContentType.I);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(3 * 1024 * 1024 + 1)]
    public async Task Users_CreateProfilePhotoUpload_Should_not_create_upload_when_size_is_invalid(long sizeInBytes)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateProfilePhotoUpload(sizeInBytes: sizeInBytes);

        // Assert
        result.ShouldBeError(InvalidProfilePhotoSize.I);
    }

    #endregion

    #region Happy path

    [Test]
    [TestCase("image/png", ".png")]
    [TestCase("image/jpeg", ".jpg")]
    [TestCase("image/webp", ".webp")]
    public async Task Users_CreateProfilePhotoUpload_Should_create_upload_url(string contentType, string extension)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateProfilePhotoUpload(contentType, sizeInBytes: 3 * 1024 * 1024);

        // Assert
        result.ShouldBeSuccess();

        var upload = result.Success;
        upload.Path.Should().EndWith(extension).And.Contain($"/{client.User.Id}/");
        upload.UploadUrl.Should().Contain("/profile-photos/");
    }

    [Test]
    public async Task Users_CreateProfilePhotoUpload_Should_create_upload_for_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateProfilePhotoUpload();

        // Assert
        result.ShouldBeSuccess();
    }

    [Test]
    public async Task Users_CreateProfilePhotoUpload_Should_create_upload_for_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateProfilePhotoUpload();

        // Assert
        result.ShouldBeSuccess();
    }

    [Test]
    public async Task Users_CreateProfilePhotoUpload_Should_create_a_distinct_path_for_each_upload()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var first = await client.CreateProfilePhotoUpload().Success();
        var second = await client.CreateProfilePhotoUpload().Success();

        // Assert
        first.Path.Should().NotBe(second.Path);
    }

    [Test]
    public async Task Users_CreateProfilePhotoUpload_Should_create_url_that_accepts_the_declared_file()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var upload = await client.CreateProfilePhotoUpload("image/png", 245_000).Success();

        // Act
        var response = await StorageFactory.Upload(upload.UploadUrl, "image/png", 245_000);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var photo = (await client.UpdateProfilePhoto(upload.Path).Success()).ProfilePhoto;
        var file = await StorageFactory.Download(photo);
        file.StatusCode.Should().Be(HttpStatusCode.OK);
        file.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        file.Content.Headers.ContentLength.Should().Be(245_000);
    }

    [Test]
    [TestCase("image/png", 245_001)]
    [TestCase("image/png", 244_999)]
    [TestCase("image/jpeg", 245_000)]
    public async Task Users_CreateProfilePhotoUpload_Should_create_url_that_rejects_a_file_different_from_the_declared(string contentType, long sizeInBytes)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var upload = await client.CreateProfilePhotoUpload("image/png", 245_000).Success();

        // Act
        var response = await StorageFactory.Upload(upload.UploadUrl, contentType, sizeInBytes);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.UpdateProfilePhoto(upload.Path)).ShouldBeError(ProfilePhotoNotFound.I);
    }

    #endregion
}

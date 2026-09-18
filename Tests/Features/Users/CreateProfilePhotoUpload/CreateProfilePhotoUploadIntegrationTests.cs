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

    #endregion
}

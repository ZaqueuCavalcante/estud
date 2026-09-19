namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("text/html")]
    [TestCase("image/gif")]
    [TestCase("image/svg+xml")]
    [TestCase("application/zip")]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_content_type_is_invalid(string? contentType)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 1, contentType: contentType!);

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkFileContentType.I);
    }

    [Test]
    [TestCase("image/png", 0)]
    [TestCase("image/png", -1)]
    [TestCase("image/png", 5 * 1024 * 1024 + 1)]
    [TestCase("image/jpeg", 5 * 1024 * 1024 + 1)]
    [TestCase("image/webp", 5 * 1024 * 1024 + 1)]
    [TestCase("application/pdf", 0)]
    [TestCase("application/pdf", 10 * 1024 * 1024 + 1)]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_size_is_invalid(string contentType, long sizeInBytes)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 1, contentType, sizeInBytes);

        // Assert
        result.ShouldBeError(InvalidClassActivityWorkFileSize.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_activity_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkFile(activityId: 999999);

        // Assert
        result.ShouldBeError(ClassActivityNotFound.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_activity_is_an_exam()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, type: ClassActivityType.Exam, weight: 40).Success();
        var student = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await student.CreateClassActivityWorkFile(activity.Id);

        // Assert
        result.ShouldBeError(ClassActivityDoesNotAcceptWorks.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateClassActivityWorkFile(activity.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_not_create_file_on_activity_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();

        var otherDirector = await _back.LoggedAsDirector();
        var otherStudent = await otherDirector.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(otherStudent.Email);

        // Act
        var result = await client.CreateClassActivityWorkFile(activity.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    [TestCase("image/png", ".png", 5 * 1024 * 1024)]
    [TestCase("image/jpeg", ".jpg", 5 * 1024 * 1024)]
    [TestCase("image/webp", ".webp", 5 * 1024 * 1024)]
    [TestCase("application/pdf", ".pdf", 10 * 1024 * 1024)]
    public async Task Students_CreateClassActivityWorkFile_Should_create_file_upload_urls(string contentType, string extension, long sizeInBytes)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.CreateClassActivityWorkFile(activity.Id, contentType, sizeInBytes);

        // Assert
        result.ShouldBeSuccess();

        var file = result.Success;
        file.PublicUrl.Should().Contain("/class-activity-work-files/").And.EndWith(extension);
        file.PublicUrl.Should().Contain($"/{@class.Id}/{activity.Id}/");
        file.UploadUrl.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Students_CreateClassActivityWorkFile_Should_create_a_distinct_url_for_each_file()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var teacher = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacher.CreateClassActivity(@class.Id, weight: 40).Success();
        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var first = await client.CreateClassActivityWorkFile(activity.Id).Success();
        var second = await client.CreateClassActivityWorkFile(activity.Id).Success();

        // Assert
        first.PublicUrl.Should().NotBe(second.PublicUrl);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("image/gif")]
    [TestCase("image/svg+xml")]
    [TestCase("application/zip")]
    [TestCase("text/html")]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_content_type_is_invalid(string? contentType)
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 1, contentType: contentType!);

        // Assert
        result.ShouldBeError(InvalidLessonPlanFileContentType.I);
    }

    [Test]
    [TestCase("image/png", 0)]
    [TestCase("image/png", -1)]
    [TestCase("image/png", 5 * 1024 * 1024 + 1)]
    [TestCase("image/jpeg", 5 * 1024 * 1024 + 1)]
    [TestCase("image/webp", 5 * 1024 * 1024 + 1)]
    [TestCase("application/pdf", 0)]
    [TestCase("application/pdf", 10 * 1024 * 1024 + 1)]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_size_is_invalid(string contentType, long sizeInBytes)
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 1, contentType, sizeInBytes);

        // Assert
        result.ShouldBeError(InvalidLessonPlanFileSize.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_when_lesson_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanFile(lessonId: 999999);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_on_lesson_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var otherTeacher = await _back.LoggedAsTeacher();

        // Act
        var result = await otherTeacher.CreateLessonPlanFile(lessons.First());

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_not_create_file_on_lesson_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.CreateLessonPlanFile(lessons.First());

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    [TestCase("image/png", ".png", 5 * 1024 * 1024)]
    [TestCase("image/jpeg", ".jpg", 5 * 1024 * 1024)]
    [TestCase("image/webp", ".webp", 5 * 1024 * 1024)]
    [TestCase("application/pdf", ".pdf", 10 * 1024 * 1024)]
    public async Task Teachers_CreateLessonPlanFile_Should_create_file_upload_urls(string contentType, string extension, long sizeInBytes)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonPlanFile(lessons.First(), contentType, sizeInBytes);

        // Assert
        result.ShouldBeSuccess();

        var file = result.Success;
        file.PublicUrl.Should().Contain("/lesson-plan-files/").And.EndWith(extension);
        file.PublicUrl.Should().Contain($"/{@class.Id}/{lessons.First()}/");
        file.UploadUrl.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Teachers_CreateLessonPlanFile_Should_create_a_distinct_url_for_each_file()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var first = await teacherClient.CreateLessonPlanFile(lessons.First()).Success();
        var second = await teacherClient.CreateLessonPlanFile(lessons.First()).Success();

        // Assert
        first.PublicUrl.Should().NotBe(second.PublicUrl);
    }

    #endregion
}

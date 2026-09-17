namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 1);

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
    [TestCase("application/pdf")]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_content_type_is_invalid(string? contentType)
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 1, contentType: contentType!);

        // Assert
        result.ShouldBeError(InvalidLessonPlanImageContentType.I);
    }

    [Test]
    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(5 * 1024 * 1024 + 1)]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_size_is_invalid(long sizeInBytes)
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 1, sizeInBytes: sizeInBytes);

        // Assert
        result.ShouldBeError(InvalidLessonPlanImageSize.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_when_lesson_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonPlanImage(lessonId: 999999);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_on_lesson_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var otherTeacher = await _back.LoggedAsTeacher();

        // Act
        var result = await otherTeacher.CreateLessonPlanImage(lessons.First());

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_not_create_image_on_lesson_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.CreateLessonPlanImage(lessons.First());

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    [TestCase("image/png", ".png")]
    [TestCase("image/jpeg", ".jpg")]
    [TestCase("image/webp", ".webp")]
    public async Task Teachers_CreateLessonPlanImage_Should_create_image_upload_urls(string contentType, string extension)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonPlanImage(lessons.First(), contentType, sizeInBytes: 5 * 1024 * 1024);

        // Assert
        result.ShouldBeSuccess();

        var image = result.Success;
        image.PublicUrl.Should().Contain("/lesson-plan-images/").And.EndWith(extension);
        image.PublicUrl.Should().Contain($"/{@class.Id}/{lessons.First()}/");
        image.UploadUrl.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Teachers_CreateLessonPlanImage_Should_create_a_distinct_url_for_each_image()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var first = await teacherClient.CreateLessonPlanImage(lessons.First()).Success();
        var second = await teacherClient.CreateLessonPlanImage(lessons.First()).Success();

        // Assert
        first.PublicUrl.Should().NotBe(second.PublicUrl);
    }

    #endregion
}

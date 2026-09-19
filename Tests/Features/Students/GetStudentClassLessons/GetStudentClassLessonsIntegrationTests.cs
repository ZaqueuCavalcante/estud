namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentClassLessons(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentClassLessons(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentClassLessons(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassLessons(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    [TestCase("gr")]
    [TestCase("  gr  ")]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_search_is_too_short(string search)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id, search: search);

        // Assert
        result.ShouldBeError(InvalidClassLessonSearch.I);
    }

    [Test]
    public async Task Students_GetStudentClassLessons_Should_not_get_lessons_when_search_is_too_long()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id, search: new string('a', 101));

        // Assert
        result.ShouldBeError(InvalidClassLessonSearch.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentClassLessons_Should_get_lessons()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id);

        // Assert
        var items = result.Success.Lessons;
        items.Should().NotBeEmpty();
        items.Should().BeInAscendingOrder(l => l.Number);
        items.Select(l => l.Id).Should().Equal(lessons);
        items.Should().OnlyContain(l => l.Status == ClassLessonStatus.Pending);
    }

    [Test]
    public async Task Students_GetStudentClassLessons_Should_search_lessons_by_planned_content()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons[0], "Lista de exercícios sobre grafos dirigidos");
        await teacherClient.UpdateLessonPlan(lessons[1], "Árvores binárias de busca");

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id, search: "exercicio grafo");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Students_GetStudentClassLessons_Should_get_all_lessons_when_search_is_blank()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons[0], "Introdução a grafos");

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLessons(@class.Id, search: "   ");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal(lessons);
    }

    #endregion
}

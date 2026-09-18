namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentClassLesson(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentClassLesson(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentClassLesson(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassLesson(999999, 1);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClassLesson(@class.Id, lessons.First());

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_when_lesson_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLesson(@class.Id, 999999);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClassLesson_Should_not_get_lesson_of_another_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var classA = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra Linear");
        var classB = await director.ShortcutCreateStartedClass(disciplineName: "Cálculo Numérico");

        var teacherClientB = await _back.LoginAs(classB.TeacherEmail);
        var lessonsB = await teacherClientB.ShortcutGetClassLessons(classB.Id);

        var client = await _back.LoginAs(classA.StudentEmail);

        // Act
        var result = await client.GetStudentClassLesson(classA.Id, lessonsB.First());

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentClassLesson_Should_get_lesson_with_planned_content()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(disciplineName: "Estrutura de Dados");

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons.First(), "## Grafos\n\n- Matriz de adjacência\n- **Dijkstra**");

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLesson(@class.Id, lessons.First());

        // Assert
        var lesson = result.Success;
        lesson.Id.Should().Be(lessons.First());
        lesson.ClassId.Should().Be(@class.Id);
        lesson.Discipline.Should().Be("Estrutura de Dados");
        lesson.Number.Should().Be(1);
        lesson.Status.Should().Be(ClassLessonStatus.Pending);
        lesson.PlannedContent.Should().Be("## Grafos\n\n- Matriz de adjacência\n- **Dijkstra**");
    }

    [Test]
    public async Task Students_GetStudentClassLesson_Should_get_lesson_without_planned_content()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClassLesson(@class.Id, lessons.First());

        // Assert
        result.Success.PlannedContent.Should().BeNull();
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherClassLesson(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherClassLesson(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassLesson(999999, 1);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_of_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, lessons.First());

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_of_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, lessons.First());

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_when_lesson_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, 999999);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_not_get_lesson_of_another_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var classA = await director.ShortcutCreateStartedClass(disciplineName: "Álgebra Linear");
        var classB = await director.ShortcutCreateStartedClass(disciplineName: "Cálculo Numérico");

        var clientB = await _back.LoginAs(classB.TeacherEmail);
        var lessonsB = await clientB.ShortcutGetClassLessons(classB.Id);

        var clientA = await _back.LoginAs(classA.TeacherEmail);

        // Act
        var result = await clientA.GetTeacherClassLesson(classA.Id, lessonsB.First());

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_get_class_lesson()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(disciplineName: "Estrutura de Dados", studentsCount: 2);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, lessons.First());

        // Assert
        var lesson = result.Success;
        lesson.Id.Should().Be(lessons.First());
        lesson.ClassId.Should().Be(@class.Id);
        lesson.Discipline.Should().Be("Estrutura de Dados");
        lesson.Number.Should().Be(1);
        lesson.Status.Should().Be(ClassLessonStatus.Pending);
        lesson.PlannedContent.Should().BeNull();
        lesson.Students.Should().HaveCount(2);
        lesson.Students.Should().OnlyContain(s => !s.Present);
        lesson.Students.Select(s => s.Id).Should().BeEquivalentTo(@class.StudentIds);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_get_class_lesson_plan()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons.First(), "## Grafos\n\n- Matriz de adjacência\n- **Dijkstra**");

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, lessons.First());

        // Assert
        result.Success.PlannedContent.Should().Be("## Grafos\n\n- Matriz de adjacência\n- **Dijkstra**");
    }

    [Test]
    public async Task Teachers_GetTeacherClassLesson_Should_get_class_lesson_attendance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var students = @class.StudentIds;

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.CreateLessonAttendance(lessons.First(), [students[0]]);

        // Act
        var result = await client.GetTeacherClassLesson(@class.Id, lessons.First());

        // Assert
        var lesson = result.Success;
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.Students.First(s => s.Id == students[0]).Present.Should().BeTrue();
        lesson.Students.First(s => s.Id == students[1]).Present.Should().BeFalse();
    }

    #endregion
}

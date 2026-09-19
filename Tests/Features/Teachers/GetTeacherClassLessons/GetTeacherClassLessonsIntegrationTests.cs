namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherClassLessons(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherClassLessons(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClassLessons(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_of_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [TestCase("gr")]
    [TestCase("  gr  ")]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_when_search_is_too_short(string search)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: search);

        // Assert
        result.ShouldBeError(InvalidClassLessonSearch.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_get_lessons_when_search_is_too_long()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: new string('a', 101));

        // Assert
        result.ShouldBeError(InvalidClassLessonSearch.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_get_lessons()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id);

        // Assert
        var lessons = result.Success.Lessons;
        lessons.Should().NotBeEmpty();
        lessons.Should().BeInAscendingOrder(l => l.Number);
        lessons.Should().AllSatisfy(l =>
        {
            l.Status.Should().Be(ClassLessonStatus.Pending);
            l.PresentStudents.Should().BeEmpty();
        });
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_get_lessons_with_present_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var students = @class.StudentIds;

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.CreateLessonAttendance(lessons.First(), [students[0]]);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id);

        // Assert
        var lesson = result.Success.Lessons.First(l => l.Id == lessons.First());
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.PresentStudents.Should().BeEquivalentTo([students[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_by_word_stem()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "## Introdução a **grafos** dirigidos");
        await client.UpdateLessonPlan(lessons[1], "Árvores binárias de busca");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_by_prefix()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Exercício número sete.");
        await client.UpdateLessonPlan(lessons[1], "Árvores binárias de busca");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "exerci");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_ignoring_accents_in_planned_content()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Lista de exercício");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "exercicio");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_ignoring_accents_in_search()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Lista de exercicio");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "exercício");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_ignoring_case()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Grafos dirigidos");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafos");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_requiring_all_words()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Grafos dirigidos");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo árvore");

        // Assert
        result.Success.Lessons.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_by_phrase()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Busca em largura");
        await client.UpdateLessonPlan(lessons[1], "Largura da busca");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "\"busca em largura\"");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_excluding_words()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Grafo dirigido");
        await client.UpdateLessonPlan(lessons[1], "Grafo ponderado");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo -ponderado");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_any_of_the_words_with_or()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Grafos dirigidos");
        await client.UpdateLessonPlan(lessons[1], "Árvores binárias");
        await client.UpdateLessonPlan(lessons[2], "Filas e pilhas");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo or árvore");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0], lessons[1]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_search_in_image_urls()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "![](https://cdn.estud.com/lessons/grafo.png)");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo");

        // Assert
        result.Success.Lessons.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_by_file_names()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Rodar o main.py no terminal");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "main.py");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_by_updated_planned_content()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Introdução a grafos");
        await client.UpdateLessonPlan(lessons[0], "Árvores binárias");

        // Act
        var oldResult = await client.GetTeacherClassLessons(@class.Id, search: "grafo");
        var newResult = await client.GetTeacherClassLessons(@class.Id, search: "árvore");

        // Assert
        oldResult.Success.Lessons.Should().BeEmpty();
        newResult.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_not_find_lessons_with_cleared_plan()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Introdução a grafos");
        await client.UpdateLessonPlan(lessons[0], null);

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafo");

        // Assert
        result.Success.Lessons.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_search_only_in_lessons_of_the_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var otherClass = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Introdução a grafos");

        var otherClient = await _back.LoginAs(otherClass.TeacherEmail);
        var otherLessons = await otherClient.ShortcutGetClassLessons(otherClass.Id);
        await otherClient.UpdateLessonPlan(otherLessons[0], "Introdução a grafos");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "grafos");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal([lessons[0]]);
    }

    [Test]
    public async Task Teachers_GetTeacherClassLessons_Should_get_all_lessons_when_search_is_blank()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await client.ShortcutGetClassLessons(@class.Id);
        await client.UpdateLessonPlan(lessons[0], "Introdução a grafos");

        // Act
        var result = await client.GetTeacherClassLessons(@class.Id, search: "   ");

        // Assert
        result.Success.Lessons.Select(l => l.Id).Should().Equal(lessons);
    }

    #endregion
}

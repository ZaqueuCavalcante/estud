namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_disciplines_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignDisciplinesToCourse(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_disciplines_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignDisciplinesToCourse(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_disciplines_to_course_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(99999, [discipline.Id]);

        // Assert
        result.ShouldBeError(CourseNotFound.I);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_disciplines_to_other_institution_course()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherCourse = await otherClient.CreateCourse().Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(otherCourse.Id, [discipline.Id]);

        // Assert
        result.ShouldBeError(CourseNotFound.I);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_disciplines_from_other_institution()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline().Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [otherDiscipline.Id]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_a_nonexistent_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var discipline = await client.CreateDiscipline().Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [discipline.Id, 999999999]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_assign_duplicate_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var discipline = await client.CreateDiscipline().Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [discipline.Id, discipline.Id]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_assign_disciplines_to_course()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var algebra = await client.CreateDiscipline("Álgebra").Success();
        var geometria = await client.CreateDiscipline("Geometria").Success();

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [algebra.Id, geometria.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetCourseDetails(course.Id);
        details.Success.Disciplines.Select(x => x.Id).Should().Equal(algebra.Id, geometria.Id);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_add_and_remove_disciplines_in_a_single_call()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var algebra = await client.CreateDiscipline("Álgebra").Success();
        var calculo = await client.CreateDiscipline("Cálculo").Success();
        var geometria = await client.CreateDiscipline("Geometria").Success();
        await client.AssignDisciplinesToCourse(course.Id, [algebra.Id, calculo.Id]);

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [calculo.Id, geometria.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetCourseDetails(course.Id);
        details.Success.Disciplines.Select(x => x.Id).Should().Equal(calculo.Id, geometria.Id);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_remove_all_disciplines_with_an_empty_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var discipline = await client.CreateDiscipline().Success();
        await client.AssignDisciplinesToCourse(course.Id, [discipline.Id]);

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetCourseDetails(course.Id);
        details.Success.Disciplines.Should().BeEmpty();
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_keep_disciplines_when_assigning_the_same_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var algebra = await client.CreateDiscipline("Álgebra").Success();
        var geometria = await client.CreateDiscipline("Geometria").Success();
        await client.AssignDisciplinesToCourse(course.Id, [algebra.Id, geometria.Id]);

        // Act
        var result = await client.AssignDisciplinesToCourse(course.Id, [algebra.Id, geometria.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetCourseDetails(course.Id);
        details.Success.Disciplines.Select(x => x.Id).Should().Equal(algebra.Id, geometria.Id);
    }

    [Test]
    public async Task Courses_AssignDisciplinesToCourse_Should_not_affect_disciplines_of_other_courses()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var administracao = await client.CreateCourse("Administração").Success();
        var biologia = await client.CreateCourse("Biologia").Success();
        var discipline = await client.CreateDiscipline().Success();
        await client.AssignDisciplinesToCourse(biologia.Id, [discipline.Id]);

        // Act
        var result = await client.AssignDisciplinesToCourse(administracao.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetCourseDetails(biologia.Id);
        details.Success.Disciplines.Select(x => x.Id).Should().Equal(discipline.Id);
    }

    #endregion
}

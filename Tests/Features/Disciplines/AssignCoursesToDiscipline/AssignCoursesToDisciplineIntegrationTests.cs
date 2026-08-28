namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_courses_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignCoursesToDiscipline(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_courses_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignCoursesToDiscipline(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_courses_to_discipline_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();

        // Act
        var result = await client.AssignCoursesToDiscipline(99999, [course.Id]);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_courses_to_other_institution_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline().Success();

        // Act
        var result = await client.AssignCoursesToDiscipline(otherDiscipline.Id, [course.Id]);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_courses_from_other_institution()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherCourse = await otherClient.CreateCourse().Success();

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, [otherCourse.Id]);

        // Assert
        result.ShouldBeError(InvalidCoursesList.I);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_assign_duplicate_courses()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var course = await client.CreateCourse().Success();

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, [course.Id, course.Id]);

        // Assert
        result.ShouldBeError(InvalidCoursesList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_assign_courses_to_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var administracao = await client.CreateCourse("Administração").Success();
        var biologia = await client.CreateCourse("Biologia").Success();

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, [administracao.Id, biologia.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Courses.Select(x => x.Id).Should().Equal(administracao.Id, biologia.Id);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_add_and_remove_courses_in_a_single_call()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var administracao = await client.CreateCourse("Administração").Success();
        var biologia = await client.CreateCourse("Biologia").Success();
        var cinema = await client.CreateCourse("Cinema").Success();
        await client.AssignCoursesToDiscipline(discipline.Id, [administracao.Id, biologia.Id]);

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, [biologia.Id, cinema.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Courses.Select(x => x.Id).Should().Equal(biologia.Id, cinema.Id);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_remove_all_courses_with_an_empty_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var course = await client.CreateCourse().Success();
        await client.AssignCoursesToDiscipline(discipline.Id, [course.Id]);

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Courses.Should().BeEmpty();
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_keep_courses_when_assigning_the_same_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var administracao = await client.CreateCourse("Administração").Success();
        var biologia = await client.CreateCourse("Biologia").Success();
        await client.AssignCoursesToDiscipline(discipline.Id, [administracao.Id, biologia.Id]);

        // Act
        var result = await client.AssignCoursesToDiscipline(discipline.Id, [administracao.Id, biologia.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Courses.Select(x => x.Id).Should().Equal(administracao.Id, biologia.Id);
    }

    [Test]
    public async Task Disciplines_AssignCoursesToDiscipline_Should_not_affect_courses_of_other_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var geometria = await client.CreateDiscipline("Geometria").Success();
        var fisica = await client.CreateDiscipline("Física").Success();
        var course = await client.CreateCourse().Success();
        await client.AssignCoursesToDiscipline(fisica.Id, [course.Id]);

        // Act
        var result = await client.AssignCoursesToDiscipline(geometria.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(fisica.Id);
        details.Success.Courses.Select(x => x.Id).Should().Equal(course.Id);
    }

    #endregion
}

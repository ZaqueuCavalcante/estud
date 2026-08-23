namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_not_get_potential_teachers_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetDisciplinePotentialTeachers(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_not_get_potential_teachers_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetDisciplinePotentialTeachers(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_not_get_potential_teachers_from_discipline_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetDisciplinePotentialTeachers(99999);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_not_get_potential_teachers_from_other_institution_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline().Success();

        // Act
        var result = await client.GetDisciplinePotentialTeachers(otherDiscipline.Id);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_get_all_teachers_when_none_linked()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        await client.CreateTeacher("Ana Lima", DataGen.Email);
        await client.CreateTeacher("Bruno Alves", DataGen.Email);

        // Act
        var result = await client.GetDisciplinePotentialTeachers(discipline.Id);

        // Assert
        var output = result.Success;
        output.Items.Should().HaveCount(2);
        output.Items.First().Name.Should().Be("Ana Lima");
        output.Items.Last().Name.Should().Be("Bruno Alves");
    }

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_not_get_already_linked_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var linkedTeacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var potentialTeacher = await client.CreateTeacher("Bruno Alves", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [linkedTeacher.Id]);

        // Act
        var result = await client.GetDisciplinePotentialTeachers(discipline.Id);

        // Assert
        var output = result.Success;
        output.Items.Should().ContainSingle();
        output.Items.First().Id.Should().Be(potentialTeacher.Id);
        output.Items.First().Name.Should().Be("Bruno Alves");
    }

    [Test]
    public async Task Disciplines_GetDisciplinePotentialTeachers_Should_filter_potential_teachers_by_name()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        await client.CreateTeacher("Ana Lima", DataGen.Email);
        await client.CreateTeacher("Bruno Alves", DataGen.Email);

        // Act
        var result = await client.GetDisciplinePotentialTeachers(discipline.Id, "bru");

        // Assert
        var output = result.Success;
        output.Items.Should().ContainSingle();
        output.Items.First().Name.Should().Be("Bruno Alves");
    }

    #endregion
}

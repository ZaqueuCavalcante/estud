namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_teachers_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignTeachersToDiscipline(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_teachers_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignTeachersToDiscipline(1, [1]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_teachers_to_discipline_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        // Act
        var result = await client.AssignTeachersToDiscipline(99999, [teacher.Id]);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_teachers_to_other_institution_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline().Success();

        // Act
        var result = await client.AssignTeachersToDiscipline(otherDiscipline.Id, [teacher.Id]);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_teachers_from_other_institution()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherTeacher = await otherClient.CreateTeacher("Bruno Alves", DataGen.Email).Success();

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, [otherTeacher.Id]);

        // Assert
        result.ShouldBeError(InvalidTeachersList.I);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_assign_duplicate_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, [teacher.Id, teacher.Id]);

        // Assert
        result.ShouldBeError(InvalidTeachersList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_assign_teachers_to_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var bruno = await client.CreateTeacher("Bruno Alves", DataGen.Email).Success();

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, [ana.Id, bruno.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Teachers.Select(x => x.Id).Should().Equal(ana.Id, bruno.Id);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_add_and_remove_teachers_in_a_single_call()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var bruno = await client.CreateTeacher("Bruno Alves", DataGen.Email).Success();
        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [ana.Id, bruno.Id]);

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, [bruno.Id, chico.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Teachers.Select(x => x.Id).Should().Equal(bruno.Id, chico.Id);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_remove_all_teachers_with_an_empty_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [teacher.Id]);

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Teachers.Should().BeEmpty();
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_keep_teachers_when_assigning_the_same_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var bruno = await client.CreateTeacher("Bruno Alves", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [ana.Id, bruno.Id]);

        // Act
        var result = await client.AssignTeachersToDiscipline(discipline.Id, [ana.Id, bruno.Id]);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(discipline.Id);
        details.Success.Teachers.Select(x => x.Id).Should().Equal(ana.Id, bruno.Id);
    }

    [Test]
    public async Task Disciplines_AssignTeachersToDiscipline_Should_not_affect_teachers_of_other_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var geometria = await client.CreateDiscipline("Geometria").Success();
        var fisica = await client.CreateDiscipline("Física").Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(fisica.Id, [ana.Id]);

        // Act
        var result = await client.AssignTeachersToDiscipline(geometria.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var details = await client.GetDisciplineDetails(fisica.Id);
        details.Success.Teachers.Select(x => x.Id).Should().Equal(ana.Id);
    }

    #endregion
}

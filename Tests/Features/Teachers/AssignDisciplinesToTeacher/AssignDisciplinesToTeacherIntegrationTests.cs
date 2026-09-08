namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_disciplines_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignDisciplinesToTeacher(1, []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_disciplines_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignDisciplinesToTeacher(1, []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_disciplines_when_teacher_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.AssignDisciplinesToTeacher(999999, []);

        // Assert
        result.ShouldBeError(TeacherNotFound.I);
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_disciplines_when_a_discipline_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [999999]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_duplicate_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var discipline = await client.CreateDiscipline("Calculo").Success();

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id, discipline.Id]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_not_assign_disciplines_from_other_institution()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline("Calculo").Success();

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [otherDiscipline.Id]);

        // Assert
        result.ShouldBeError(InvalidDisciplinesList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_assign_disciplines_to_the_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var calculo = await client.CreateDiscipline("Calculo").Success();
        var fisica = await client.CreateDiscipline("Fisica").Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [calculo.Id, fisica.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacher(teacher.Id).Success();
        updated.Disciplines.Should().HaveCount(2);
        updated.Disciplines.Should().Contain(x => x.Id == calculo.Id);
        updated.Disciplines.Should().Contain(x => x.Id == fisica.Id);
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_add_and_remove_disciplines_in_a_single_call()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var algebra = await client.CreateDiscipline("Algebra").Success();
        var calculo = await client.CreateDiscipline("Calculo").Success();
        var fisica = await client.CreateDiscipline("Fisica").Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [algebra.Id, calculo.Id]);

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [calculo.Id, fisica.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacher(teacher.Id).Success();
        updated.Disciplines.Select(x => x.Id).Should().Equal(calculo.Id, fisica.Id);
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_remove_all_disciplines_with_an_empty_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var calculo = await client.CreateDiscipline("Calculo").Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [calculo.Id]);

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacher(teacher.Id).Success();
        updated.Disciplines.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_AssignDisciplinesToTeacher_Should_keep_disciplines_when_assigning_the_same_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var calculo = await client.CreateDiscipline("Calculo").Success();
        var fisica = await client.CreateDiscipline("Fisica").Success();
        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [calculo.Id, fisica.Id]);

        // Act
        var result = await client.AssignDisciplinesToTeacher(teacher.Id, [calculo.Id, fisica.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetTeacher(teacher.Id).Success();
        updated.Disciplines.Select(x => x.Id).Should().Equal(calculo.Id, fisica.Id);
    }

    #endregion
}

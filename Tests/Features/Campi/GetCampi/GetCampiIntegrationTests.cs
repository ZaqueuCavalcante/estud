namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Campi_GetCampi_Should_not_get_campi_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCampi();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Campi_GetCampi_Should_not_get_campi_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCampi();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Campi_GetCampi_Should_get_campi()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        await client.CreateCampus("Suassuna", BrazilState.PB, "João Pessoa");
        await client.CreateCampus("Agreste", BrazilState.PE, "Caruaru");

        // Act
        var result = await client.GetCampi();

        // Assert
        result.Success.Total.Should().Be(2);

        var first = result.Success.Items.First();
        first.Id.Should().BeGreaterThan(0);
        first.Name.Should().Be("Agreste");
        first.City.Should().Be("Caruaru");
        first.State.Should().Be(BrazilState.PE);
        first.UsedMinutesRate.Should().Be(0);
        first.UsedCapacityRate.Should().Be(0);

        var last = result.Success.Items.Last();
        last.Id.Should().BeGreaterThan(0);
        last.Name.Should().Be("Suassuna");
        last.City.Should().Be("João Pessoa");
        last.State.Should().Be(BrazilState.PB);
        last.UsedMinutesRate.Should().Be(0);
        last.UsedCapacityRate.Should().Be(0);
    }

    [Test]
    public async Task Campi_GetCampi_Should_get_campi_used_rates()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var agreste = await client.CreateCampus("Agreste", BrazilState.PE, "Caruaru").Success();
        var suassuna = await client.CreateCampus("Suassuna", BrazilState.PB, "João Pessoa").Success();

        var salaAgreste = await client.CreateClassroom(agreste.Id, name: "Sala 01", capacity: 8).Success();
        await client.CreateClassroom(suassuna.Id, name: "Sala 02", capacity: 8);

        await client.UpdateCampusOpeningHours(agreste.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
        ]);

        await client.UpdateCampusOpeningHours(suassuna.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: agreste.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H12_00, null, salaAgreste.Id)]);

        var studentA = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentB = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(studentA.Id, @class.Id).Success();
        await client.AssignStudentToClass(studentB.Id, @class.Id).Success();

        // Act
        var result = await client.GetCampi();

        // Assert
        result.Success.Total.Should().Be(2);

        // 5h x 60min = 300min usados
        // 11h x 60min = 660min disponíveis
        var first = result.Success.Items.First();
        first.Name.Should().Be("Agreste");
        first.UsedMinutesRate.Should().Be(45.45M);

        // 2 alunos x 300min = 600 assento-minutos usados
        // 8 lugares x 660min = 5.280 assento-minutos disponíveis
        first.UsedCapacityRate.Should().Be(11.36M);

        // Mesma sala e mesmos horários de funcionamento, mas nenhuma turma alocada.
        var last = result.Success.Items.Last();
        last.Name.Should().Be("Suassuna");
        last.UsedMinutesRate.Should().Be(0);
        last.UsedCapacityRate.Should().Be(0);
    }

    #endregion
}

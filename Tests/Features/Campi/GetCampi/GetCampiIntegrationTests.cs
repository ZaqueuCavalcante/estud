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

    #endregion
}

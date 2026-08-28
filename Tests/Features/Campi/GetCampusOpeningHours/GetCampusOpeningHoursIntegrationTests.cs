namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_not_get_campus_opening_hours_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCampusOpeningHours(campusId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_not_get_campus_opening_hours_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCampusOpeningHours(campusId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_not_get_campus_opening_hours_when_campus_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCampusOpeningHours(campusId: 99999);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_not_get_other_institution_campus_opening_hours()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();

        // Act
        var result = await client.GetCampusOpeningHours(otherCampus.Id);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_get_default_opening_hours_of_a_new_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Campus Agreste").Success();

        // Act
        var result = await client.GetCampusOpeningHours(campus.Id);

        // Assert
        var openingHours = result.Success;
        openingHours.CampusId.Should().Be(campus.Id);
        openingHours.Campus.Should().Be("Campus Agreste");

        openingHours.Days.Should().HaveCount(6);
        openingHours.Days.Select(d => d.Day).Should().Equal(
            Day.Monday, Day.Tuesday, Day.Wednesday, Day.Thursday, Day.Friday, Day.Saturday);

        // Segunda a sexta, 07:00–22:00: manhã, tarde e noite.
        foreach (var day in openingHours.Days.Where(d => d.Day != Day.Saturday))
        {
            day.Windows.Should().HaveCount(3);
            day.Windows[0].Start.Should().Be(Hour.H07_00);
            day.Windows[0].End.Should().Be(Hour.H12_00);
            day.Windows[1].Start.Should().Be(Hour.H13_00);
            day.Windows[1].End.Should().Be(Hour.H18_00);
            day.Windows[2].Start.Should().Be(Hour.H19_00);
            day.Windows[2].End.Should().Be(Hour.H22_00);
        }

        // Sábado nasce fechado — lista de janelas vazia.
        openingHours.Days.First(d => d.Day == Day.Saturday).Windows.Should().BeEmpty();
    }

    [Test]
    public async Task Campi_GetCampusOpeningHours_Should_get_opening_hours_of_each_campus_separately()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Agreste").Success();
        var otherCampus = await client.CreateCampus(name: "Suassuna", city: "Recife").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Act
        var result = await client.GetCampusOpeningHours(otherCampus.Id);

        // Assert
        var openingHours = result.Success;
        openingHours.Days.First(d => d.Day == Day.Monday).Windows.Should().HaveCount(3);
        openingHours.Days.First(d => d.Day == Day.Monday).Windows[0].End.Should().Be(Hour.H12_00);
        openingHours.Days.First(d => d.Day == Day.Friday).Windows.Should().HaveCount(3);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateCampusOpeningHours(campusId: 1,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateCampusOpeningHours(campusId: 1,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_campus_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateCampusOpeningHours(campusId: 99999,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_other_institution_campus_opening_hours()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(otherCampus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_window_ends_before_it_starts()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H12_00, Hour.H07_00)]),
        ]);

        // Assert
        result.ShouldBeError(InvalidOpeningHour.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_window_is_empty()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H07_00)]),
        ]);

        // Assert
        result.ShouldBeError(InvalidOpeningHour.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_windows_overlap()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H11_00, Hour.H18_00)]),
        ]);

        // Assert
        result.ShouldBeError(OverlappingOpeningHours.I);
    }

    // A segunda está correta e a colisão está na terça: um dia válido não pode
    // mascarar o inválido, e a checagem varre todos os dias do payload.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_windows_overlap_in_another_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H10_00), (Hour.H13_00, Hour.H18_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H11_00), (Hour.H10_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(OverlappingOpeningHours.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_the_same_window_is_repeated()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H07_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(OverlappingOpeningHours.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_window_crosses_two_shifts()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H11_00, Hour.H14_00)]),
        ]);

        // Assert
        result.ShouldBeError(OpeningHourOutsideShift.I);
    }

    // 07:00–22:00 cobre manhã, tarde e noite: uma janela só não pode varrer o dia
    // inteiro, precisa ser quebrada em uma por turno.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_window_spans_the_whole_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H22_00)]),
        ]);

        // Assert
        result.ShouldBeError(OpeningHourOutsideShift.I);
    }

    // 00:00 fica antes do início da manhã (06:00), então a janela não cai em turno nenhum.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_window_starts_before_the_first_shift()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H00_00, Hour.H10_00)]),
        ]);

        // Assert
        result.ShouldBeError(OpeningHourOutsideShift.I);
    }

    // As duas janelas cabem na manhã e não se sobrepõem, mas o turno só comporta uma.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_a_shift_has_two_windows()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H09_00), (Hour.H10_00, Hour.H12_00)]),
        ]);

        // Assert
        result.ShouldBeError(MultipleOpeningHoursInShift.I);
    }

    // A segunda tem uma janela por turno e a colisão está na terça: um dia válido
    // não pode mascarar o inválido.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_a_shift_has_two_windows_in_another_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H13_00, Hour.H18_00)]),
            (Day.Tuesday, [(Hour.H13_00, Hour.H15_00), (Hour.H16_00, Hour.H18_00)]),
        ]);

        // Assert
        result.ShouldBeError(MultipleOpeningHoursInShift.I);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_update_campus_opening_hours_when_day_is_repeated()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Monday, [(Hour.H14_00, Hour.H18_00)]),
        ]);

        // Assert
        result.ShouldBeError(InvalidOpeningHoursList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_close_saturday_and_open_it_in_the_morning()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H13_00, Hour.H18_00), (Hour.H19_00, Hour.H22_00)]),
            (Day.Saturday, [(Hour.H08_00, Hour.H12_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        var saturday = openingHours.Days.First(d => d.Day == Day.Saturday);
        saturday.Windows.Should().HaveCount(1);
        saturday.Windows[0].Start.Should().Be(Hour.H08_00);
        saturday.Windows[0].End.Should().Be(Hour.H12_00);

        // Terça a sexta não vieram no PUT, então fecharam — replace-all.
        openingHours.Days.Where(d => d.Day is Day.Tuesday or Day.Wednesday or Day.Thursday or Day.Friday)
            .Should().OnlyContain(d => d.Windows.Count == 0);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_update_campus_opening_hours_with_two_windows_in_the_same_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H14_00, Hour.H18_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        var monday = openingHours.Days.First(d => d.Day == Day.Monday);
        monday.Windows.Should().HaveCount(2);
        monday.Windows[0].Start.Should().Be(Hour.H07_00);
        monday.Windows[0].End.Should().Be(Hour.H12_00);
        monday.Windows[1].Start.Should().Be(Hour.H14_00);
        monday.Windows[1].End.Should().Be(Hour.H18_00);
    }

    // Duas janelas no mesmo intervalo em dias diferentes não colidem: a checagem
    // de sobreposição é por dia, não pela lista inteira.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_update_campus_opening_hours_with_two_windows_in_more_than_one_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H10_00), (Hour.H13_00, Hour.H18_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H10_00), (Hour.H13_00, Hour.H18_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        foreach (var day in openingHours.Days.Where(d => d.Day is Day.Monday or Day.Tuesday))
        {
            day.Windows.Should().HaveCount(2);
            day.Windows[0].Start.Should().Be(Hour.H07_00);
            day.Windows[0].End.Should().Be(Hour.H10_00);
            day.Windows[1].Start.Should().Be(Hour.H13_00);
            day.Windows[1].End.Should().Be(Hour.H18_00);
        }
    }

    // 07:00–12:00 e 12:00–18:00 se encostam sem se sobrepor — o fim de uma é o
    // início da outra. É a fronteira onde um `<=` no lugar do `<` erraria.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_update_campus_opening_hours_with_adjacent_windows()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        var monday = openingHours.Days.First(d => d.Day == Day.Monday);
        monday.Windows.Should().HaveCount(2);
        monday.Windows[0].End.Should().Be(Hour.H12_00);
        monday.Windows[1].Start.Should().Be(Hour.H12_00);
    }

    // O limite é uma janela por turno, não uma por dia: três janelas cabem no dia
    // desde que cada uma fique dentro de um turno diferente.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_update_campus_opening_hours_with_one_window_per_shift()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H13_00, Hour.H18_00), (Hour.H19_00, Hour.H22_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        var monday = openingHours.Days.First(d => d.Day == Day.Monday);
        monday.Windows.Should().HaveCount(3);
        monday.Windows[0].Start.Should().Be(Hour.H07_00);
        monday.Windows[0].End.Should().Be(Hour.H12_00);
        monday.Windows[1].Start.Should().Be(Hour.H13_00);
        monday.Windows[1].End.Should().Be(Hour.H18_00);
        monday.Windows[2].Start.Should().Be(Hour.H19_00);
        monday.Windows[2].End.Should().Be(Hour.H22_00);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_replace_the_previous_opening_hours_instead_of_accumulating()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H18_00, Hour.H22_00)]),
        ]);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();

        var monday = openingHours.Days.First(d => d.Day == Day.Monday);
        monday.Windows.Should().HaveCount(1);
        monday.Windows[0].Start.Should().Be(Hour.H18_00);
        monday.Windows[0].End.Should().Be(Hour.H22_00);
    }

    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_close_the_campus_for_the_whole_week()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.UpdateCampusOpeningHours(campus.Id, []);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var openingHours = await client.GetCampusOpeningHours(campus.Id).Success();
        openingHours.Days.Should().HaveCount(6);
        openingHours.Days.Should().OnlyContain(d => d.Windows.Count == 0);
    }

    // A configuração vive só no nível do campus, sem herança: um PUT num campus
    // não pode, por construção, alcançar outro.
    [Test]
    public async Task Campi_UpdateCampusOpeningHours_Should_not_change_the_opening_hours_of_another_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Agreste").Success();
        var otherCampus = await client.CreateCampus(name: "Suassuna", city: "Recife").Success();

        // Act
        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Saturday, [(Hour.H08_00, Hour.H12_00)]),
        ]);

        // Assert
        var openingHours = await client.GetCampusOpeningHours(otherCampus.Id).Success();

        openingHours.Days.First(d => d.Day == Day.Saturday).Windows.Should().BeEmpty();
        openingHours.Days.Where(d => d.Day != Day.Saturday)
            .Should().OnlyContain(d => d.Windows.Count == 3);
    }

    #endregion
}

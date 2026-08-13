namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Calendar_GetCalendar_Should_not_get_calendar_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCalendar();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Calendar_GetCalendar_Should_not_get_calendar_when_user_is_not_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCalendar();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Calendar_GetCalendar_Should_not_get_calendar_of_a_campus_that_does_not_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026, campusId: 159);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_not_get_calendar_of_a_campus_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var campus = await director.CreateCampus().Success();

        var other = await _back.LoggedAsDirector();

        // Act
        var result = await other.GetCalendar(2026, campus.Id);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Calendar_GetCalendar_Should_get_all_days_of_the_year()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026);

        // Assert
        var calendar = result.Success;
        calendar.Year.Should().Be(2026);
        calendar.Total.Should().Be(365);
        calendar.Items.Should().HaveCount(365);
        calendar.Items[0].Date.Should().Be(new DateTime(2026, 1, 1));
        calendar.Items[^1].Date.Should().Be(new DateTime(2026, 12, 31));
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_get_all_days_of_a_leap_year()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2028).Success();

        // Assert
        result.Total.Should().Be(366);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_get_the_current_year_when_no_year_is_informed()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar().Success();

        // Assert
        result.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_mark_national_holidays()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026);

        // Assert
        var calendar = result.Success;

        var christmas = calendar.Items.First(x => x.Date == new DateTime(2026, 12, 25));
        christmas.DayType.Should().Be(DayType.Holiday);
        christmas.Description.Should().Be("Natal");

        // Páscoa de 2026: 05/04. Sexta-feira Santa: 03/04.
        var goodFriday = calendar.Items.First(x => x.Date == new DateTime(2026, 4, 3));
        goodFriday.DayType.Should().Be(DayType.Holiday);
        goodFriday.Description.Should().Be("Sexta-feira Santa");
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_mark_weekends()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026);

        // Assert
        var calendar = result.Success;

        // 14/03/2026 é um sábado e 15/03/2026 um domingo.
        calendar.Items.First(x => x.Date == new DateTime(2026, 3, 14)).DayType.Should().Be(DayType.Weekend);
        calendar.Items.First(x => x.Date == new DateTime(2026, 3, 15)).DayType.Should().Be(DayType.Weekend);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_mark_holidays_that_fall_on_a_weekend_as_holidays()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026);

        // Assert
        // 15/11/2026 (Proclamação da República) cai num domingo: o feriado prevalece sobre o fim de semana.
        var holiday = result.Success.Items.First(x => x.Date == new DateTime(2026, 11, 15));
        holiday.DayType.Should().Be(DayType.Holiday);
        holiday.Description.Should().Be("Proclamação da República");
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_mark_common_days_as_default()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026);

        // Assert
        var day = result.Success.Items.First(x => x.Date == new DateTime(2026, 3, 10));
        day.DayType.Should().Be(DayType.Default);
        day.Description.Should().BeNull();
        day.Source.Should().Be(CalendarDaySource.Default);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_let_the_institution_override_a_national_holiday()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // 12/10/2026 é Nossa Senhora Aparecida.
        await client.CreateCalendarDay(new DateTime(2026, 10, 12), DayType.Default, "Dia letivo").Success();

        // Act
        var result = await client.GetCalendar(2026).Success();

        // Assert
        var day = result.Items.First(x => x.Date == new DateTime(2026, 10, 12));
        day.DayType.Should().Be(DayType.Default);
        day.Description.Should().Be("Dia letivo");
        day.Source.Should().Be(CalendarDaySource.Institution);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_let_the_campus_override_the_institution()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var date = new DateTime(2026, 11, 9);

        await client.CreateCalendarDay(date, DayType.Vacation, "Férias").Success();
        await client.CreateCalendarDay(date, DayType.Default, "Aula normal aqui", campusId: campus.Id).Success();

        // Act
        var result = await client.GetCalendar(2026, campus.Id).Success();

        // Assert
        var day = result.Items.First(x => x.Date == date);
        day.DayType.Should().Be(DayType.Default);
        day.Description.Should().Be("Aula normal aqui");
        day.Source.Should().Be(CalendarDaySource.Campus);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_inherit_the_institution_day_in_a_campus_without_override()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var date = new DateTime(2026, 11, 10);

        await client.CreateCalendarDay(date, DayType.Recess, "Recesso").Success();

        // Act
        var result = await client.GetCalendar(2026, campus.Id).Success();

        // Assert
        var day = result.Items.First(x => x.Date == date);
        day.DayType.Should().Be(DayType.Recess);
        day.Source.Should().Be(CalendarDaySource.Institution);
        // O id é nulo porque o override não é daquele campus: dali só dá para sobrescrever.
        day.Id.Should().BeNull();
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_not_mix_days_of_different_campi()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var first = await client.CreateCampus("Agreste I").Success();
        var second = await client.CreateCampus("Agreste II").Success();
        var date = new DateTime(2026, 11, 11);

        await client.CreateCalendarDay(date, DayType.Holiday, "Feriado local", campusId: first.Id).Success();

        // Act
        var result = await client.GetCalendar(2026, second.Id).Success();

        // Assert
        var day = result.Items.First(x => x.Date == date);
        day.DayType.Should().Be(DayType.Default);
        day.Source.Should().Be(CalendarDaySource.Default);
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_return_the_campus_of_the_calendar()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus("Agreste I").Success();

        // Act
        var result = await client.GetCalendar(2026, campus.Id).Success();

        // Assert
        result.CampusId.Should().Be(campus.Id);
        result.Campus.Should().Be("Agreste I");
    }

    [Test]
    public async Task Calendar_GetCalendar_Should_not_return_a_campus_in_the_institution_calendar()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCalendar(2026).Success();

        // Assert
        result.CampusId.Should().BeNull();
        result.Campus.Should().BeNull();
    }

    #endregion
}

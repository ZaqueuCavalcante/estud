namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateCalendarDay();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_when_user_is_not_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateCalendarDay();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_with_invalid_type()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(dayType: null);

        // Assert
        result.ShouldBeError(InvalidCalendarDayType.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_as_weekend()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(dayType: DayType.Weekend);

        // Assert
        result.ShouldBeError(InvalidCalendarDayType.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_with_too_long_description()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(description: new string('a', 101));

        // Assert
        result.ShouldBeError(InvalidCalendarDayDescription.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_range_that_ends_before_it_starts()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(
            date: new DateTime(2026, 2, 20),
            endDate: new DateTime(2026, 2, 10)
        );

        // Assert
        result.ShouldBeError(InvalidCalendarDayRange.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_range_longer_than_a_year()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(
            date: new DateTime(2026, 1, 1),
            endDate: new DateTime(2027, 6, 1)
        );

        // Assert
        result.ShouldBeError(InvalidCalendarDayRange.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_of_a_campus_that_does_not_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(campusId: 159);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_of_a_campus_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var campus = await director.CreateCampus().Success();

        var other = await _back.LoggedAsDirector();

        // Act
        var result = await other.CreateCalendarDay(campusId: campus.Id);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_day_that_already_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var date = new DateTime(2026, 2, 10);
        await client.CreateCalendarDay(date);

        // Act
        var result = await client.CreateCalendarDay(date);

        // Assert
        result.ShouldBeError(CalendarDayAlreadyExists.I);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_create_range_that_touches_an_existing_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateCalendarDay(new DateTime(2026, 8, 12));

        // Act
        var result = await client.CreateCalendarDay(
            date: new DateTime(2026, 8, 10),
            endDate: new DateTime(2026, 8, 14)
        );

        // Assert
        result.ShouldBeError(CalendarDayAlreadyExists.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_create_the_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(new DateTime(2026, 3, 16), DayType.Recess, "Semana de provas").Success();

        // Assert
        result.Total.Should().Be(1);
        result.Ids.Should().HaveCount(1);

        var calendar = await client.GetCalendar(2026).Success();
        var day = calendar.Items.First(x => x.Date == new DateTime(2026, 3, 16));
        day.Id.Should().Be(result.Ids[0]);
        day.DayType.Should().Be(DayType.Recess);
        day.Description.Should().Be("Semana de provas");
        day.Source.Should().Be(CalendarDaySource.Institution);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_create_a_range_of_days()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCalendarDay(
            date: new DateTime(2026, 1, 5),
            dayType: DayType.Vacation,
            description: "Férias de verão",
            endDate: new DateTime(2026, 1, 9)
        ).Success();

        // Assert
        result.Total.Should().Be(5);
        result.Ids.Should().HaveCount(5);

        var calendar = await client.GetCalendar(2026).Success();
        var days = calendar.Items
            .Where(x => x.Date >= new DateTime(2026, 1, 5) && x.Date <= new DateTime(2026, 1, 9))
            .ToList();

        days.Should().OnlyContain(x => x.DayType == DayType.Vacation);
        days.Should().OnlyContain(x => x.Description == "Férias de verão");
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_create_a_school_day_on_a_weekend()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        // 09/05/2026 é um sábado.
        await client.CreateCalendarDay(new DateTime(2026, 5, 9), DayType.Default, "Reposição de aulas").Success();

        // Assert
        var calendar = await client.GetCalendar(2026).Success();
        var day = calendar.Items.First(x => x.Date == new DateTime(2026, 5, 9));
        day.DayType.Should().Be(DayType.Default);
        day.Source.Should().Be(CalendarDaySource.Institution);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_create_the_day_of_a_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.CreateCalendarDay(
            date: new DateTime(2026, 3, 19),
            dayType: DayType.Recess,
            description: "Aniversário da cidade",
            campusId: campus.Id
        ).Success();

        // Assert
        var campusCalendar = await client.GetCalendar(2026, campus.Id).Success();
        var campusDay = campusCalendar.Items.First(x => x.Date == new DateTime(2026, 3, 19));
        campusDay.Id.Should().Be(result.Ids[0]);
        campusDay.DayType.Should().Be(DayType.Recess);
        campusDay.Source.Should().Be(CalendarDaySource.Campus);

        // O dia do campus não vaza para o calendário da instituição.
        var calendar = await client.GetCalendar(2026).Success();
        var day = calendar.Items.First(x => x.Date == new DateTime(2026, 3, 19));
        day.Id.Should().BeNull();
        day.DayType.Should().Be(DayType.Default);
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_create_the_same_day_in_both_levels()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var date = new DateTime(2026, 9, 14);

        await client.CreateCalendarDay(date, DayType.Vacation, "Férias").Success();

        // Act
        var result = await client.CreateCalendarDay(date, DayType.Recess, "Recesso local", campusId: campus.Id);

        // Assert
        // O override do campus convive com o da instituição: níveis diferentes não colidem.
        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Calendar_CreateCalendarDay_Should_not_see_days_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.CreateCalendarDay(new DateTime(2026, 4, 15), DayType.Vacation, "Férias");

        var other = await _back.LoggedAsDirector();

        // Act
        var calendar = await other.GetCalendar(2026).Success();

        // Assert
        var day = calendar.Items.First(x => x.Date == new DateTime(2026, 4, 15));
        day.Id.Should().BeNull();
        day.DayType.Should().Be(DayType.Default);
    }

    #endregion
}

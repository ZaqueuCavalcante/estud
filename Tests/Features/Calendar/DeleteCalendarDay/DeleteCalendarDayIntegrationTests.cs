namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_not_delete_day_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.DeleteCalendarDay(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_not_delete_day_when_user_is_not_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.DeleteCalendarDay(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_not_delete_day_that_does_not_exists()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.DeleteCalendarDay(159);

        // Assert
        result.ShouldBeError(CalendarDayNotFound.I);
    }

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_not_delete_day_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var day = await director.CreateCalendarDay(new DateTime(2026, 10, 5)).Success();

        var other = await _back.LoggedAsDirector();

        // Act
        var result = await other.DeleteCalendarDay(day.Ids[0]);

        // Assert
        result.ShouldBeError(CalendarDayNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_make_the_day_inherit_again()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var date = new DateTime(2026, 10, 6);
        var day = await client.CreateCalendarDay(date, DayType.Vacation, "Férias").Success();

        // Act
        await client.DeleteCalendarDay(day.Ids[0]).Success();

        // Assert
        var calendar = await client.GetCalendar(2026).Success();
        var item = calendar.Items.First(x => x.Date == date);
        item.Id.Should().BeNull();
        item.DayType.Should().Be(DayType.Default);
        item.Source.Should().Be(CalendarDaySource.Default);
    }

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_make_the_day_go_back_to_the_national_holiday()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var christmas = new DateTime(2026, 12, 25);
        var day = await client.CreateCalendarDay(christmas, DayType.Default, "Vamos trabalhar").Success();

        // Act
        await client.DeleteCalendarDay(day.Ids[0]).Success();

        // Assert
        var calendar = await client.GetCalendar(2026).Success();
        var item = calendar.Items.First(x => x.Date == christmas);
        item.DayType.Should().Be(DayType.Holiday);
        item.Description.Should().Be("Natal");
        item.Source.Should().Be(CalendarDaySource.Global);
    }

    [Test]
    public async Task Calendar_DeleteCalendarDay_Should_make_the_campus_day_go_back_to_the_institution_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var date = new DateTime(2026, 10, 7);

        await client.CreateCalendarDay(date, DayType.Vacation, "Férias").Success();
        var campusDay = await client.CreateCalendarDay(date, DayType.Default, "Aula normal aqui", campusId: campus.Id).Success();

        // Act
        await client.DeleteCalendarDay(campusDay.Ids[0]).Success();

        // Assert
        var calendar = await client.GetCalendar(2026, campus.Id).Success();
        var item = calendar.Items.First(x => x.Date == date);
        item.Id.Should().BeNull();
        item.DayType.Should().Be(DayType.Vacation);
        item.Description.Should().Be("Férias");
        item.Source.Should().Be(CalendarDaySource.Institution);
    }

    #endregion
}

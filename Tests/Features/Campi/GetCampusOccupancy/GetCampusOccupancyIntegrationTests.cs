namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_get_campus_occupancy_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCampusOccupancy(campusId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_get_campus_occupancy_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCampusOccupancy(campusId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_get_campus_occupancy_when_campus_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCampusOccupancy(campusId: 99999);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_get_other_institution_campus_occupancy()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();

        // Act
        var result = await client.GetCampusOccupancy(otherCampus.Id);

        // Assert
        result.ShouldBeError(CampusNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_campus_occupancy_without_allocated_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Campus Agreste").Success();
        await client.CreateClassroom(campus.Id, name: "Sala 05");
        await client.CreateClassroom(campus.Id, name: "Sala 06");

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.CampusId.Should().Be(campus.Id);
        occupancy.Campus.Should().Be("Campus Agreste");
        occupancy.TotalClassrooms.Should().Be(2);
        occupancy.OverallRate.Should().Be(0);

        // 6 dias x 3 turnos, cada célula detalhada pelas 2 salas do campus
        occupancy.Cells.Should().HaveCount(18);
        occupancy.Cells.Should().OnlyContain(c => c.Classrooms.Count == 2);
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);

        // Campus novo nasce seg–sex, 07:00–22:00: sábado fica fora do denominador.
        occupancy.OpenCells.Should().Be(15);
        occupancy.Cells.Where(c => c.Day == Day.Saturday)
            .Should().OnlyContain(c => !c.Open && c.AvailableMinutes == 0);

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.Open.Should().BeTrue();
        morning.AvailableMinutes.Should().Be(600);

        // A noite vai até 24h, mas o campus fecha às 22h: 240min, não 360.
        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.AvailableMinutes.Should().Be(480);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_campus_occupancy_with_allocated_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();
        var sala02 = await client.CreateClassroom(campus.Id, name: "Sala 02").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // O segundo horário cruza a fronteira manhã/tarde de propósito: 10h–14h
        // são 120min de manhã (até 12h) e 120min à tarde.
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, null),
            (Day.Monday, Hour.H10_00, Hour.H14_00, null),
        ]);
        await client.UpdateClassClassrooms(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, sala01.Id),
            (Day.Monday, Hour.H10_00, Hour.H14_00, sala02.Id),
        ]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(300);      // 180 na Sala 01 + 120 na Sala 02
        morning.AvailableMinutes.Should().Be(600); // 2 salas x 300min
        morning.Rate.Should().Be(50M);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(180);
        morning.Classrooms.First(c => c.Id == sala01.Id).Rate.Should().Be(60M);
        morning.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(120);
        morning.Classrooms.First(c => c.Id == sala02.Id).Rate.Should().Be(40M);

        // A tarde fica só com o rabo do horário que cruzou o meio-dia.
        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.UsedMinutes.Should().Be(120);
        afternoon.AvailableMinutes.Should().Be(720);
        afternoon.Rate.Should().Be(16.67M);
        afternoon.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(0);
        afternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(120);
        afternoon.Classrooms.First(c => c.Id == sala02.Id).Rate.Should().Be(33.33M);

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.UsedMinutes.Should().Be(0);

        // Nenhum outro dia da semana tem alocação.
        occupancy.Cells.Where(c => c.Day != Day.Monday).Should().OnlyContain(c => c.UsedMinutes == 0);

        // 420min de 9000min na semana: 2 salas x 900min/dia (07h–22h) x 5 dias abertos.
        // O sábado não entra — o campus não abre, e horário que não existe não é folga.
        occupancy.OverallRate.Should().Be(4.67M);
        occupancy.OpenCells.Should().Be(15);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_count_classrooms_of_other_campi()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Agreste").Success();
        await client.CreateClassroom(campus.Id, name: "Sala 01");

        var otherCampus = await client.CreateCampus(name: "Suassuna", city: "Recife").Success();
        var otherClassroom = await client.CreateClassroom(otherCampus.Id, name: "Sala 02").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: otherCampus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null)]);
        await client.UpdateClassClassrooms(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, otherClassroom.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.OverallRate.Should().Be(0);
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_campus_occupancy_without_classrooms()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.TotalClassrooms.Should().Be(0);
        occupancy.OverallRate.Should().Be(0);
        occupancy.Cells.Should().HaveCount(18);
        occupancy.Cells.Should().OnlyContain(c => c.AvailableMinutes == 0 && c.Classrooms.Count == 0);

        // Sem sala o denominador é zero, mas o campus continua abrindo — Open não
        // fala de sala, fala de funcionamento.
        occupancy.OpenCells.Should().Be(15);
    }

    // O caso que motivou a feature: campus só de manhã tinha o denominador inflado
    // em 4x, e o gestor lia folga onde não havia.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_campus_occupancy_of_a_morning_only_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();
        await client.CreateClassroom(campus.Id, name: "Sala 02");

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Wednesday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Thursday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Friday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H14_00, null)]);
        await client.UpdateClassClassrooms(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H14_00, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        occupancy.OpenCells.Should().Be(5);
        occupancy.Cells.Where(c => c.Shift != Shift.Morning || c.Day == Day.Saturday)
            .Should().OnlyContain(c => !c.Open && c.AvailableMinutes == 0 && c.UsedMinutes == 0);

        // A aula vai até 14h, mas o campus fecha ao meio-dia: só 300min contam.
        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.Open.Should().BeTrue();
        morning.UsedMinutes.Should().Be(300);
        morning.AvailableMinutes.Should().Be(600); // 2 salas x 300min abertos
        morning.Rate.Should().Be(50M);

        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.Open.Should().BeFalse();
        afternoon.UsedMinutes.Should().Be(0);

        // 300min de 3000min (2 salas x 300min x 5 dias). Com o denominador de um
        // campus seg–sáb nos três turnos inteiros (12240min) o mesmo uso daria 2,45%.
        occupancy.OverallRate.Should().Be(10M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_count_schedules_outside_the_campus_opening_hours()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H13_00, Hour.H18_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null)]);
        await client.UpdateClassClassrooms(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // Aula cadastrada num horário que o campus não tem não é ocupação de nada.
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);
        occupancy.OverallRate.Should().Be(0);
        occupancy.OpenCells.Should().Be(1);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_clip_the_schedule_that_crosses_the_end_of_the_opening_hours()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H22_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H21_00, Hour.H23_00, null)]);
        await client.UpdateClassClassrooms(@class.Id, [(Day.Monday, Hour.H21_00, Hour.H23_00, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // 21h–23h numa noite que acaba às 22h: só a primeira hora existe.
        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.UsedMinutes.Should().Be(60);
        evening.AvailableMinutes.Should().Be(240); // 1 sala x 18h–22h
        evening.Rate.Should().Be(25M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_use_the_opening_hours_of_each_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Agreste").Success();
        await client.CreateClassroom(campus.Id, name: "Sala 01");

        var otherCampus = await client.CreateCampus(name: "Suassuna", city: "Recife").Success();
        await client.CreateClassroom(otherCampus.Id, name: "Sala 02");

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Saturday, [(Hour.H08_00, Hour.H12_00)]),
        ]);

        // Act
        var occupancy = await client.GetCampusOccupancy(campus.Id).Success();
        var otherOccupancy = await client.GetCampusOccupancy(otherCampus.Id).Success();

        // Assert
        occupancy.OpenCells.Should().Be(1);
        occupancy.Cells.First(c => c.Day == Day.Saturday && c.Shift == Shift.Morning)
            .AvailableMinutes.Should().Be(240);

        // O outro campus continua com o padrão do seed, intocado.
        otherOccupancy.OpenCells.Should().Be(15);
        otherOccupancy.Cells.First(c => c.Day == Day.Saturday && c.Shift == Shift.Morning)
            .Open.Should().BeFalse();
    }

    #endregion
}

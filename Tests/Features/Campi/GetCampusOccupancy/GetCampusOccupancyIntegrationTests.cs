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
        await client.CreateCampus();

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
    public async Task Campi_GetCampusOccupancy_Should_get_small_campus_occupancy_with_allocated_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, name: "Sala 01", capacity: 6).Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00), (Hour.H18_00, Hour.H22_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H12_00, null, classroom.Id)]);

        var studentA = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentB = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentC = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(studentA.Id, @class.Id).Success();
        await client.AssignStudentToClass(studentB.Id, @class.Id).Success();
        await client.AssignStudentToClass(studentC.Id, @class.Id).Success();

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // 300min usados de 900min abertos: só a segunda abre, e nos três turnos.
        occupancy.OverallUsedMinutesRate.Should().Be(33.33M);

        // Em assento-minuto: 900 usados (3 alunos x 300min) de 5400 ofertados
        // (6 lugares x 900min). A sala lota de horário sem lotar de gente.
        occupancy.OverallUsedCapacityRate.Should().Be(16.67M);

        // A sala enche de horário na manhã, mas só com metade dos assentos:
        // 3 alunos numa sala de 6 lugares.
        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        var sala = morning.Classrooms.First(c => c.Id == classroom.Id);
        sala.UsedMinutesRate.Should().Be(100M);
        sala.UsedCapacityRate.Should().Be(50M);

        // Sala única no campus: a taxa de assentos da célula é a da sala.
        morning.UsedCapacityRate.Should().Be(50M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_weigh_cell_capacity_by_classroom_size()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var pequena = await client.CreateClassroom(campus.Id, name: "Sala 01", capacity: 10).Success();
        await client.CreateClassroom(campus.Id, name: "Auditório", capacity: 90);

        await client.UpdateCampusOpeningHours(campus.Id, [(Day.Monday, [(Hour.H07_00, Hour.H12_00)])]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H12_00, null, pequena.Id)]);

        var studentA = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentB = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(studentA.Id, @class.Id).Success();
        await client.AssignStudentToClass(studentB.Id, @class.Id).Success();

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);

        // 2 alunos numa sala de 10 lugares, o dia inteiro do turno; o auditório de
        // 90 lugares fica vazio.
        morning.Classrooms.First(c => c.Id == pequena.Id).UsedCapacityRate.Should().Be(20M);
        morning.Classrooms.First(c => c.Id == pequena.Id).AverageStudents.Should().Be(2);
        morning.Classrooms.First(c => c.Name == "Auditório").AverageStudents.Should().Be(0);

        // Assento-minuto: 600 usados (2 alunos x 300min) de 30000 ofertados
        // (100 lugares x 300min). A média das taxas das salas daria 10% — o
        // auditório vazio pesa mais do que a salinha cheia.
        morning.UsedCapacity.Should().Be(600);
        morning.UsedCapacityRate.Should().Be(2M);

        // A janela do turno, sem multiplicar pelas salas: 07h–12h.
        morning.OpenMinutes.Should().Be(300);
        morning.AvailableMinutes.Should().Be(600);

        // Só a manhã de segunda abre, então o total da semana é o da célula.
        occupancy.OverallUsedCapacityRate.Should().Be(2M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_week_occupancy_of_each_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01", capacity: 3).Success();
        var sala02 = await client.CreateClassroom(campus.Id, name: "Sala 02", capacity: 10).Success();

        // Segunda e terça de manhã: 600min abertos na semana, 300 em cada dia.
        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
            (Day.Tuesday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // A turma só usa a Sala 01, e só na segunda: metade da semana do campus.
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H12_00, null, sala01.Id)]);

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(student.Id, @class.Id).Success();

        // Act
        var occupancy = await client.GetCampusOccupancy(campus.Id).Success();

        // Assert
        occupancy.Classrooms.Should().HaveCount(2);
        occupancy.Classrooms.Select(c => c.Name).Should().ContainInOrder("Sala 01", "Sala 02");

        var sala01Week = occupancy.Classrooms.First(c => c.Id == sala01.Id);
        sala01Week.Capacity.Should().Be(3);
        sala01Week.AvailableMinutes.Should().Be(600);
        sala01Week.UsedMinutes.Should().Be(300);
        sala01Week.UsedMinutesRate.Should().Be(50M);

        // 1 aluno x 300min de 3 lugares x 600min abertos. Em lugares: 0,5 assento
        // ocupado em média — a sala fica vazia metade da semana, mas o movimento
        // que existe não pode virar 0.
        sala01Week.UsedCapacity.Should().Be(300);
        sala01Week.UsedCapacityRate.Should().Be(16.67M);
        sala01Week.AverageStudents.Should().Be(1);

        // Sala nunca alocada zera as duas taxas, mas continua com denominador.
        var sala02Week = occupancy.Classrooms.First(c => c.Id == sala02.Id);
        sala02Week.AvailableMinutes.Should().Be(600);
        sala02Week.UsedMinutesRate.Should().Be(0M);
        sala02Week.UsedCapacityRate.Should().Be(0M);
        sala02Week.AverageStudents.Should().Be(0);
    }





















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
        occupancy.OverallUsedMinutesRate.Should().Be(0);

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

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.AvailableMinutes.Should().Be(360);
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
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // O segundo horário cruza a fronteira manhã/tarde de propósito: 10h–14h
        // são 120min de manhã (até 12h) e 60min à tarde porque entre 12h e 13h é almoço.
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, null, sala01.Id),
            (Day.Monday, Hour.H10_00, Hour.H14_00, null, sala02.Id),
        ]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(300);      // 180 na Sala 01 + 120 na Sala 02
        morning.AvailableMinutes.Should().Be(600); // 2 salas x 300min
        morning.UsedMinutesRate.Should().Be(50M);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(180);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutesRate.Should().Be(60M);
        morning.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(120);
        morning.Classrooms.First(c => c.Id == sala02.Id).UsedMinutesRate.Should().Be(40M);

        // A tarde fica só com o rabo do horário que cruzou o meio-dia.
        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.UsedMinutes.Should().Be(60);
        afternoon.AvailableMinutes.Should().Be(600);
        afternoon.UsedMinutesRate.Should().Be(10.00M);
        afternoon.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(0);
        afternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutes.Should().Be(60);
        afternoon.Classrooms.First(c => c.Id == sala02.Id).UsedMinutesRate.Should().Be(20.00M);

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.UsedMinutes.Should().Be(0);

        // Nenhum outro dia da semana tem alocação.
        occupancy.Cells.Where(c => c.Day != Day.Monday).Should().OnlyContain(c => c.UsedMinutes == 0);

        // 360min de 7.800min na semana: 2 salas x 13h x 60min x 5 dias abertos.
        occupancy.OverallUsedMinutesRate.Should().Be(4.62M);
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
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: otherCampus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, otherClassroom.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.OverallUsedMinutesRate.Should().Be(0);
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
        occupancy.OverallUsedMinutesRate.Should().Be(0);
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
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H14_00, null, sala01.Id)]);

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
        morning.UsedMinutesRate.Should().Be(50M);

        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.Open.Should().BeFalse();
        afternoon.UsedMinutes.Should().Be(0);

        // 300min de 3000min (2 salas x 300min x 5 dias). Com o denominador de um
        // campus seg–sáb nos três turnos inteiros (12240min) o mesmo uso daria 2,45%.
        occupancy.OverallUsedMinutesRate.Should().Be(10M);
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
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // Aula cadastrada num horário que o campus não tem não é ocupação de nada.
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);
        occupancy.OverallUsedMinutesRate.Should().Be(0);
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
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H12_00, Hour.H18_00), (Hour.H18_00, Hour.H22_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H21_00, Hour.H23_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // 21h–23h numa noite que acaba às 22h: só a primeira hora existe.
        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.UsedMinutes.Should().Be(60);
        evening.AvailableMinutes.Should().Be(240); // 1 sala x 18h–22h
        evening.UsedMinutesRate.Should().Be(25M);
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

    // Uma sala costuma receber várias turmas ao longo do turno, e a célula precisa
    // somar todas — não é "a turma daquele horário", é quanto da sala foi usado.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_sum_the_schedules_of_different_classes_in_the_same_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();

        var classA = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await client.UpdateClassSchedules(classA.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, null, sala01.Id)]);

        var classB = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await client.UpdateClassSchedules(classB.Id, [(Day.Monday, Hour.H10_00, Hour.H12_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // 120min de cada turma na mesma sala e na mesma célula.
        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(240);
        morning.AvailableMinutes.Should().Be(300);
        morning.UsedMinutesRate.Should().Be(80M);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutes.Should().Be(240);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutesRate.Should().Be(80M);

        // 240min de 3.900min (1 sala x 13h x 60min x 5 dias abertos).
        occupancy.OverallUsedMinutesRate.Should().Be(6.15M);
    }

    // Turma iniciada é turma acontecendo: sai do mapa só quando finaliza.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_count_the_schedules_of_a_started_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, sala01.Id)]);

        await client.ReleaseClassForEnrollment(@class.Id);
        await client.StartClass(@class.Id);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(180);
        morning.AvailableMinutes.Should().Be(300);
        morning.UsedMinutesRate.Should().Be(60M);
    }

    // Turma finalizada é histórico: a sala dela está livre para o próximo período,
    // então continuar ocupando o mapa seria mentir sobre a capacidade do campus.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_count_the_schedules_of_a_finalized_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, sala01.Id)]);

        // Não existe endpoint de finalizar turma, então o status vai direto no banco.
        await using (var ctx = _back.GetDbContext())
        {
            var entity = await ctx.Classes.FirstAsync(c => c.Id == @class.Id);
            entity.Status = ClassStatus.Finalized;
            await ctx.SaveChangesAsync();
        }

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);
        occupancy.OverallUsedMinutesRate.Should().Be(0);
    }

    // Turma online tem horário mas não tem sala: existe na agenda, não no mapa.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_count_schedules_without_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        await client.CreateClassroom(campus.Id, name: "Sala 01");

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // Só o horário: sem o passo de alocar sala, o ClassroomId fica nulo.
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.Cells.Should().OnlyContain(c => c.UsedMinutes == 0);
        occupancy.OverallUsedMinutesRate.Should().Be(0);
    }

    // Campus que fecha para o almoço tem duas janelas no mesmo dia, e o turno inteiro
    // que cai no intervalo entre elas fica fechado — mesmo cercado por turnos abertos.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_sum_the_opening_hours_windows_of_the_same_day()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00), (Hour.H18_00, Hour.H22_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // 11h–19h atravessa as duas janelas e o buraco entre elas.
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H11_00, Hour.H19_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // Manhã e noite abrem; a tarde inteira cai no intervalo fechado.
        occupancy.OpenCells.Should().Be(2);

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.Open.Should().BeTrue();
        morning.AvailableMinutes.Should().Be(300);
        morning.UsedMinutes.Should().Be(60);  // 11h–12h
        morning.UsedMinutesRate.Should().Be(20M);

        var afternoon = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon);
        afternoon.Open.Should().BeFalse();
        afternoon.AvailableMinutes.Should().Be(0);
        afternoon.UsedMinutes.Should().Be(0);

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.AvailableMinutes.Should().Be(240);
        evening.UsedMinutes.Should().Be(60);  // 18h–19h
        evening.UsedMinutesRate.Should().Be(25M);

        // 120min de 540min: as duas janelas do dia, e só elas.
        occupancy.OverallUsedMinutesRate.Should().Be(22.22M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_campus_occupancy_of_a_campus_closed_all_week()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, sala01.Id)]);

        // Campus fechado depois da turma já alocada.
        await client.UpdateCampusOpeningHours(campus.Id, []);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // A sala continua existindo; o que sumiu foi o horário em que ela poderia ser usada.
        occupancy.TotalClassrooms.Should().Be(1);
        occupancy.OpenCells.Should().Be(0);
        occupancy.Cells.Should().HaveCount(18);
        occupancy.Cells.Should().OnlyContain(c => !c.Open && c.AvailableMinutes == 0 && c.UsedMinutes == 0);
        occupancy.OverallUsedMinutesRate.Should().Be(0);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_a_fully_occupied_cell()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H07_00, Hour.H12_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        // A aula cobre exatamente a única janela do campus.
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H12_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        var morning = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning);
        morning.UsedMinutes.Should().Be(300);
        morning.AvailableMinutes.Should().Be(300);
        morning.UsedMinutesRate.Should().Be(100M);
        morning.Classrooms.First(c => c.Id == sala01.Id).UsedMinutesRate.Should().Be(100M);

        occupancy.OpenCells.Should().Be(1);
        occupancy.OverallUsedMinutesRate.Should().Be(100M);
    }

    [Test]
    public async Task Campi_GetCampusOccupancy_Should_count_the_schedules_of_saturday()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Saturday, [(Hour.H08_00, Hour.H12_00)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Saturday, Hour.H08_00, Hour.H10_00, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // Sábado é fechado por padrão, mas quando o campus abre ele conta como qualquer outro dia.
        var morning = occupancy.Cells.First(c => c.Day == Day.Saturday && c.Shift == Shift.Morning);
        morning.Open.Should().BeTrue();
        morning.AvailableMinutes.Should().Be(240);
        morning.UsedMinutes.Should().Be(120);
        morning.UsedMinutesRate.Should().Be(50M);

        occupancy.OpenCells.Should().Be(1);
        occupancy.OverallUsedMinutesRate.Should().Be(50M);
    }

    // Horário que encosta na fronteira do turno pertence a um turno só: 12h–14h é
    // tarde inteira, e 07h–12h é manhã inteira. Nenhum dos dois vaza para o vizinho.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_not_count_a_schedule_that_ends_where_the_shift_starts()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H12_00, Hour.H14_00, null, sala01.Id),
            (Day.Tuesday, Hour.H07_00, Hour.H12_00, null, sala01.Id),
        ]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        // Começa às 12h em ponto: nada na manhã.
        occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Morning)
            .UsedMinutes.Should().Be(0);
        occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Afternoon)
            .UsedMinutes.Should().Be(60);

        // Termina às 12h em ponto: nada na tarde.
        occupancy.Cells.First(c => c.Day == Day.Tuesday && c.Shift == Shift.Morning)
            .UsedMinutes.Should().Be(300);
        occupancy.Cells.First(c => c.Day == Day.Tuesday && c.Shift == Shift.Afternoon)
            .UsedMinutes.Should().Be(0);
    }

    // A noite do mapa vai das 18h à meia-noite, e não até as 22h do padrão: campus
    // que fecha mais tarde tem mais denominador, não uma célula estourada.
    [Test]
    public async Task Campi_GetCampusOccupancy_Should_get_the_evening_of_a_campus_open_after_22h()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var sala01 = await client.CreateClassroom(campus.Id, name: "Sala 01").Success();

        await client.UpdateCampusOpeningHours(campus.Id,
        [
            (Day.Monday, [(Hour.H18_00, Hour.H23_45)]),
        ]);

        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H22_00, Hour.H23_45, null, sala01.Id)]);

        // Act
        var result = await client.GetCampusOccupancy(campus.Id);

        // Assert
        var occupancy = result.Success;

        var evening = occupancy.Cells.First(c => c.Day == Day.Monday && c.Shift == Shift.Evening);
        evening.Open.Should().BeTrue();
        evening.AvailableMinutes.Should().Be(345);  // 18h–23h45, e não os 240 do padrão
        evening.UsedMinutes.Should().Be(105);
        evening.UsedMinutesRate.Should().Be(30.43M);

        occupancy.OpenCells.Should().Be(1);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classrooms_GetClassroom_Should_not_get_classroom_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetClassroom(classroomId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classrooms_GetClassroom_Should_not_get_classroom_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetClassroom(classroomId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classrooms_GetClassroom_Should_not_get_classroom_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetClassroom(classroomId: 99999);

        // Assert
        result.ShouldBeError(ClassroomNotFound.I);
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_not_get_other_institution_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();
        var otherClassroom = await otherClient.CreateClassroom(otherCampus.Id).Success();

        // Act
        var result = await client.GetClassroom(otherClassroom.Id);

        // Assert
        result.ShouldBeError(ClassroomNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus(name: "Campus Agreste").Success();
        var classroom = await client.CreateClassroom(campus.Id, name: "Sala 05", capacity: 40).Success();

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.Id.Should().Be(classroom.Id);
        found.Name.Should().Be("Sala 05");
        found.CampusId.Should().Be(campus.Id);
        found.Campus.Should().Be("Campus Agreste");
        found.Capacity.Should().Be(40);
        found.Schedules.Should().BeEmpty();
        found.ClassesCount.Should().Be(0);
        found.WeeklyHours.Should().Be(0);
        found.PeakStudents.Should().Be(0);
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom_with_allocated_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, capacity: 40).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, null, classroom.Id),
            (Day.Wednesday, Hour.H07_00, Hour.H09_00, null, classroom.Id),
        ]);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.Schedules.Should().HaveCount(2);
        found.ClassesCount.Should().Be(1);
        found.WeeklyHours.Should().Be(5M);
        found.PeakStudents.Should().Be(0);

        found.Schedules[0].ClassId.Should().Be(@class.Id);
        found.Schedules[0].Discipline.Should().Be("Geometria");
        found.Schedules[0].Period.Should().Be(period.Name);
        found.Schedules[0].Status.Should().Be(ClassStatus.OnPreEnrollment);
        found.Schedules[0].Students.Should().Be(0);
        found.Schedules[0].Day.Should().Be(Day.Monday);
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom_with_enrolled_students_count()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, capacity: 40).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, classroom.Id)]);
        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.PeakStudents.Should().Be(1);
        found.Schedules.Should().ContainSingle();
        found.Schedules[0].Students.Should().Be(1);
        found.Schedules[0].Status.Should().Be(ClassStatus.OnPreEnrollment);
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom_with_class_teachers_ordered_by_name()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);
        await client.AssignDisciplinesToTeacher(ana.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, classroom.Id)]);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.Schedules.Should().ContainSingle();
        found.Schedules[0].Teachers.Should().Equal("Ana Lima", "Chico Ferreira");
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom_with_multiple_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, capacity: 40).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var geometria = await client.CreateDiscipline("Geometria").Success();
        var calculo = await client.CreateDiscipline("Cálculo I").Success();

        var morning = await client.CreateClass(geometria.Id, period.Id).Success();
        var evening = await client.CreateClass(calculo.Id, period.Id).Success();

        await client.UpdateClassSchedules(morning.Id, [(Day.Wednesday, Hour.H07_00, Hour.H10_00, null, classroom.Id)]);
        await client.UpdateClassSchedules(evening.Id, [(Day.Monday, Hour.H19_00, Hour.H21_00, null, classroom.Id)]);

        var maria = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var joao = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var ana = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(maria.Id, morning.Id);
        await client.AssignStudentToClass(joao.Id, morning.Id);
        await client.AssignStudentToClass(ana.Id, evening.Id);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.ClassesCount.Should().Be(2);
        found.WeeklyHours.Should().Be(5M);
        found.PeakStudents.Should().Be(2);

        found.Schedules.Should().HaveCount(2);
        found.Schedules[0].Day.Should().Be(Day.Monday);
        found.Schedules[0].ClassId.Should().Be(evening.Id);
        found.Schedules[0].Discipline.Should().Be("Cálculo I");
        found.Schedules[0].Students.Should().Be(1);
        found.Schedules[1].Day.Should().Be(Day.Wednesday);
        found.Schedules[1].ClassId.Should().Be(morning.Id);
        found.Schedules[1].Discipline.Should().Be("Geometria");
        found.Schedules[1].Students.Should().Be(2);
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_get_classroom_with_started_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id)]);
        await client.ReleaseClassForEnrollment(@class.Id);
        await client.StartClass(@class.Id);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.Schedules.Should().ContainSingle();
        found.Schedules[0].Status.Should().Be(ClassStatus.Started);
        found.Schedules[0].Teachers.Should().Equal("Chico Ferreira");
    }

    [Test]
    public async Task Classrooms_GetClassroom_Should_not_get_schedules_allocated_in_another_classroom()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, name: "Sala 05").Success();
        var otherClassroom = await client.CreateClassroom(campus.Id, name: "Sala 06").Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, null, classroom.Id),
            (Day.Tuesday, Hour.H07_00, Hour.H09_00, null, otherClassroom.Id),
        ]);

        // Act
        var result = await client.GetClassroom(classroom.Id);

        // Assert
        var found = result.Success;
        found.Schedules.Should().ContainSingle();
        found.Schedules[0].Day.Should().Be(Day.Monday);
        found.ClassesCount.Should().Be(1);
        found.WeeklyHours.Should().Be(3M);
    }

    #endregion
}

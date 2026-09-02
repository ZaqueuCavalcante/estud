namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_not_get_agenda_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_not_get_agenda_when_user_is_a_manager()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_not_get_agenda_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_teacher_has_no_started_classes()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_agenda_of_a_class_with_a_single_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(@class.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
        days[0].Disciplines[0].Start.Should().Be(Hour.H07_00);
        days[0].Disciplines[0].End.Should().Be(Hour.H10_00);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_build_a_separate_agenda_for_each_teacher_of_a_two_teacher_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var ana = await director.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var chico = await director.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(ana.Id, [discipline.Id]);
        await director.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        await director.UpdateClassTeachers(@class.Id, [ana.Id, chico.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, ana.Id, null),
            (Day.Tuesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H12_00, chico.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        var anaClient = await _back.LoginAs(ana.Email);
        var chicoClient = await _back.LoginAs(chico.Email);

        // Act
        var anaAgenda = await anaClient.GetTeacherAgenda().Success();
        var chicoAgenda = await chicoClient.GetTeacherAgenda().Success();

        // Assert
        var anaDays = anaAgenda.Days;
        anaDays.Should().HaveCount(2);
        anaDays.Select(d => d.Day).Should().Equal(Day.Monday, Day.Tuesday);

        var chicoDays = chicoAgenda.Days;
        chicoDays.Should().HaveCount(1);
        chicoDays[0].Day.Should().Be(Day.Wednesday);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_order_the_disciplines_of_a_day_by_start_hour()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [geometria.Id, algebra.Id]);

        var morningClass = await director.CreateClass(geometria.Id, period.Id).Success();
        await director.UpdateClassTeachers(morningClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(morningClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(morningClass.Id);
        await director.StartClass(morningClass.Id);

        var nightClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(nightClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(nightClass.Id, [(Day.Monday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(nightClass.Id);
        await director.StartClass(nightClass.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.Start).Should().Equal(Hour.H07_00, Hour.H19_00);
        days[0].Disciplines.Select(d => d.End).Should().Equal(Hour.H09_00, Hour.H22_00);
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(morningClass.Id, nightClass.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
        days[0].Disciplines[1].Name.Should().Be("Álgebra");
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_show_the_classroom_name_when_the_schedule_has_a_classroom_and_null_when_online()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var campus = await director.CreateCampus().Success();
        var classroom = await director.CreateClassroom(campus.Id, "Sala 07").Success();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, teacher.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(2);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines[0].ClassroomName.Should().Be("Sala 07");
        days[1].Day.Should().Be(Day.Wednesday);
        days[1].Disciplines[0].ClassroomName.Should().BeNull();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_the_class_is_on_pre_enrollment()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_the_class_is_on_enrollment()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(@class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_the_class_is_finalized()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        // TODO: use finalize class endpoint
        await using (var ctx = _back.GetDbContext())
        {
            var entity = await ctx.Classes.FirstAsync(c => c.Id == @class.Id);
            entity.Status = ClassStatus.Finalized;
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_only_the_started_class_when_the_teacher_also_has_a_not_started_one()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [geometria.Id, algebra.Id]);

        var startedClass = await director.CreateClass(geometria.Id, period.Id).Success();
        await director.UpdateClassTeachers(startedClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(startedClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(startedClass.Id);
        await director.StartClass(startedClass.Id);

        var notStartedClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(notStartedClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(notStartedClass.Id, [(Day.Wednesday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(notStartedClass.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(startedClass.Id);
        days[0].Disciplines[0].Name.Should().Be("Geometria");
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_the_teacher_is_on_the_class_but_has_no_schedules()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var ana = await director.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var chico = await director.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(ana.Id, [discipline.Id]);
        await director.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [ana.Id, chico.Id]);
        await director.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null)]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        var client = await _back.LoginAs(ana.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        result.Success.Days.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_not_get_the_schedules_without_a_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, null, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_empty_agenda_when_the_teacher_is_removed_from_a_started_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var ana = await director.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var chico = await director.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(ana.Id, [discipline.Id]);
        await director.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [ana.Id, chico.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, ana.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H12_00, chico.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        await director.UpdateClassTeachers(@class.Id, [chico.Id]).Success();

        var anaClient = await _back.LoginAs(ana.Email);
        var chicoClient = await _back.LoginAs(chico.Email);

        // Act
        var anaAgenda = await anaClient.GetTeacherAgenda().Success();
        var chicoAgenda = await chicoClient.GetTeacherAgenda().Success();

        // Assert
        anaAgenda.Days.Should().BeEmpty();

        var chicoDays = chicoAgenda.Days;
        chicoDays.Should().HaveCount(1);
        chicoDays[0].Day.Should().Be(Day.Wednesday);
        chicoDays[0].Disciplines.Should().HaveCount(1);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_order_the_days_by_week_day_when_the_classes_are_created_out_of_order()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [geometria.Id, algebra.Id]);

        var fridayClass = await director.CreateClass(geometria.Id, period.Id).Success();
        await director.UpdateClassTeachers(fridayClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(fridayClass.Id, [(Day.Friday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(fridayClass.Id);
        await director.StartClass(fridayClass.Id);

        var mondayClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(mondayClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(mondayClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(mondayClass.Id);
        await director.StartClass(mondayClass.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(2);
        days.Select(d => d.Day).Should().Equal(Day.Monday, Day.Friday);
        days[0].Disciplines[0].ClassId.Should().Be(mondayClass.Id);
        days[1].Disciplines[0].ClassId.Should().Be(fridayClass.Id);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_both_schedules_when_the_class_has_two_schedules_on_the_same_day()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null),
            (Day.Monday, Hour.H10_00, Hour.H12_00, teacher.Id, null),
        ]);
        await director.ReleaseClassForEnrollment(@class.Id);
        await director.StartClass(@class.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(@class.Id, @class.Id);
        days[0].Disciplines.Select(d => d.Start).Should().Equal(Hour.H07_00, Hour.H10_00);
        days[0].Disciplines.Select(d => d.End).Should().Equal(Hour.H09_00, Hour.H12_00);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_get_two_classes_of_the_same_discipline_on_the_same_day()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline("Geometria").Success();
        var period = await director.GetFirstAcademicPeriod();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var morningClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(morningClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(morningClass.Id, [(Day.Monday, Hour.H07_00, Hour.H09_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(morningClass.Id);
        await director.StartClass(morningClass.Id);

        var nightClass = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(nightClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(nightClass.Id, [(Day.Monday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(nightClass.Id);
        await director.StartClass(nightClass.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Disciplines.Should().HaveCount(2);
        days[0].Disciplines.Select(d => d.Name).Should().Equal("Geometria", "Geometria");
        days[0].Disciplines.Select(d => d.ClassId).Should().Equal(morningClass.Id, nightClass.Id);
    }

    [Test]
    public async Task Teachers_GetTeacherAgenda_Should_not_get_the_classes_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var otherDirector = await _back.LoggedAsDirector();
        await otherDirector.ShortcutCreateStartedClass(students: [], day: Day.Wednesday);

        var client = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await client.GetTeacherAgenda();

        // Assert
        result.ShouldBeSuccess();
        var days = result.Success.Days;
        days.Should().HaveCount(1);
        days[0].Day.Should().Be(Day.Monday);
        days[0].Disciplines.Should().HaveCount(1);
        days[0].Disciplines[0].ClassId.Should().Be(@class.Id);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_GetClassSchedules_Should_not_get_schedules_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetClassSchedules(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_GetClassSchedules_Should_not_get_schedules_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetClassSchedules(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_GetClassSchedules_Should_not_get_schedules_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetClassSchedules(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_GetClassSchedules_Should_get_the_class_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var classroom = await client.CreateClassroom(campus.Id, name: "Sala 05").Success();
        var discipline = await client.CreateDiscipline("Banco de Dados").Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var @class = await client.CreateClass(discipline.Id, period.Id, campusId: campus.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, null, null),
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id),
        ]);

        // Act
        var result = await client.GetClassSchedules(@class.Id);

        // Assert
        var schedules = result.Success.Schedules;
        schedules.Should().HaveCount(2);
        schedules.Should().OnlyContain(s => s.ClassId == @class.Id && !s.FromOtherClass && s.Discipline == "Banco de Dados");

        schedules[0].Day.Should().Be(Day.Monday);
        schedules[0].StartAt.Should().Be(Hour.H07_00);
        schedules[0].EndAt.Should().Be(Hour.H10_00);
        schedules[0].TeacherId.Should().Be(teacher.Id);
        schedules[0].Teacher.Should().Be("Ana Lima");
        schedules[0].ClassroomId.Should().Be(classroom.Id);
        schedules[0].Classroom.Should().Be("Sala 05");

        schedules[1].Day.Should().Be(Day.Wednesday);
        schedules[1].TeacherId.Should().BeNull();
        schedules[1].Teacher.Should().BeNull();
        schedules[1].ClassroomId.Should().BeNull();
        schedules[1].Classroom.Should().BeNull();
    }

    [Test]
    public async Task Classes_GetClassSchedules_Should_get_an_empty_list_when_the_class_has_no_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.GetClassSchedules(@class.Id);

        // Assert
        result.Success.Schedules.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_GetClassSchedules_Should_include_the_schedules_of_the_class_teachers_in_other_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline("Banco de Dados").Success();
        var otherDiscipline = await client.CreateDiscipline("Estruturas de Dados").Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var teacher = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id, otherDiscipline.Id]);

        var otherClass = await client.CreateClass(otherDiscipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(otherClass.Id, [teacher.Id]);
        await client.UpdateClassSchedules(otherClass.Id, [(Day.Tuesday, Hour.H19_00, Hour.H22_00, teacher.Id, null)]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacher.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        // Act
        var result = await client.GetClassSchedules(@class.Id);

        // Assert
        var schedules = result.Success.Schedules;
        schedules.Should().HaveCount(2);

        schedules[0].ClassId.Should().Be(@class.Id);
        schedules[0].FromOtherClass.Should().BeFalse();

        schedules[1].ClassId.Should().Be(otherClass.Id);
        schedules[1].FromOtherClass.Should().BeTrue();
        schedules[1].Discipline.Should().Be("Estruturas de Dados");
        schedules[1].Day.Should().Be(Day.Tuesday);
        schedules[1].StartAt.Should().Be(Hour.H19_00);
        schedules[1].EndAt.Should().Be(Hour.H22_00);
        schedules[1].TeacherId.Should().Be(teacher.Id);
        schedules[1].Teacher.Should().Be("Ana Lima");
    }

    [Test]
    public async Task Classes_GetClassSchedules_Should_include_only_the_slots_of_the_class_teachers_from_other_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [ana.Id, chico.Id]);

        var otherClass = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(otherClass.Id, [ana.Id, chico.Id]);
        await client.UpdateClassSchedules(otherClass.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, ana.Id, null),
            (Day.Tuesday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, null, null),
        ]);

        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [ana.Id]);

        // Act
        var result = await client.GetClassSchedules(@class.Id);

        // Assert
        var schedules = result.Success.Schedules;
        schedules.Should().ContainSingle();
        schedules[0].ClassId.Should().Be(otherClass.Id);
        schedules[0].FromOtherClass.Should().BeTrue();
        schedules[0].Day.Should().Be(Day.Monday);
        schedules[0].TeacherId.Should().Be(ana.Id);
    }

    [Test]
    public async Task Classes_GetClassSchedules_Should_not_include_the_schedules_of_finalized_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var finalized = await client.ShortcutCreateStartedClass(students: [], day: Day.Friday);
        await client.FinalizeClass(finalized.Id).Success();

        var finalizedClass = await client.GetClass(finalized.Id).Success();
        var teacherId = finalizedClass.Teachers.Single().Id;
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var @class = await client.CreateClass(finalizedClass.DisciplineId, period.Id).Success();
        await client.UpdateClassTeachers(@class.Id, [teacherId]);

        // Act
        var result = await client.GetClassSchedules(@class.Id);

        // Assert
        result.Success.Schedules.Should().BeEmpty();
    }

    #endregion
}

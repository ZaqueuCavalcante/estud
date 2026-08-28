namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateClassTeachers(classId: 1, teachers: []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateClassTeachers(classId: 1, teachers: []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateClassTeachers(classId: 999999, teachers: []);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_list_has_more_than_two_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var first = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var second = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var third = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, teachers: [first.Id, second.Id, third.Id]);

        // Assert
        result.ShouldBeError(InvalidTeachersList.I);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_list_has_duplicated_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var teacher = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, teachers: [teacher.Id, teacher.Id]);

        // Assert
        result.ShouldBeError(InvalidTeachersList.I);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_a_teacher_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        var teacher = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, teachers: [teacher.Id, 999999]);

        // Assert
        result.ShouldBeError(TeacherNotFound.I);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_update_teachers_when_a_teacher_is_not_assigned_to_the_class_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline("Geometria").Success();
        var otherDiscipline = await client.CreateDiscipline("Fisica").Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [otherDiscipline.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, teachers: [chico.Id]);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToDiscipline.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_add_the_first_teacher_keeping_no_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().Equal(chico.Id);
        updated.Schedules.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_auto_assign_the_added_teacher_to_existing_teacherless_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, null, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, null, null),
        ]);

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().Equal(chico.Id);
        updated.Schedules.Should().HaveCount(2);
        updated.Schedules.Should().OnlyContain(s => s.TeacherId == null);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_swap_the_single_teacher_keeping_no_schedules()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [ana.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().Equal(ana.Id);
        updated.Schedules.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_reassign_schedules_to_the_new_single_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, chico.Id, null),
        ]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [ana.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().Equal(ana.Id);
        updated.Schedules.Should().HaveCount(2);
        updated.Schedules.Should().OnlyContain(s => s.TeacherId == null);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_clear_the_schedule_teacher_when_removing_the_last_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, null, null)]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Should().BeEmpty();
        updated.Schedules.Should().ContainSingle();
        updated.Schedules[0].TeacherId.Should().BeNull();
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_keep_the_schedule_teacher_when_adding_a_second_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id]);
        await client.UpdateClassSchedules(@class.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null)]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().BeEquivalentTo([chico.Id, ana.Id]);
        updated.Schedules.Should().ContainSingle();
        updated.Schedules[0].TeacherId.Should().Be(chico.Id);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_be_idempotent_when_reassigning_the_same_single_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);
        await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().Equal(chico.Id);
        updated.Schedules.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_orphan_only_the_removed_teacher_schedule_when_swapping_one_of_two()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var joao = await client.CreateTeacher("João Alves", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id, joao.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
        ]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id, joao.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().BeEquivalentTo([chico.Id, joao.Id]);
        updated.Schedules.Single(s => s.Day == Day.Monday).TeacherId.Should().Be(chico.Id);
        updated.Schedules.Single(s => s.Day == Day.Wednesday).TeacherId.Should().BeNull();
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_orphan_all_schedules_when_swapping_both_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var joao = await client.CreateTeacher("João Alves", DataGen.Email).Success();
        var maria = await client.CreateTeacher("Maria Souza", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id, joao.Id, maria.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
        ]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [joao.Id, maria.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().BeEquivalentTo([joao.Id, maria.Id]);
        updated.Schedules.Should().HaveCount(2);
        updated.Schedules.Should().OnlyContain(s => s.TeacherId == null);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_clear_all_schedule_teachers_when_removing_both_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
        ]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, []);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Should().BeEmpty();
        updated.Schedules.Should().HaveCount(2);
        updated.Schedules.Should().OnlyContain(s => s.TeacherId == null);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_keep_all_schedules_when_reassigning_the_same_two_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
        ]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [ana.Id, chico.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().BeEquivalentTo([chico.Id, ana.Id]);
        updated.Schedules.Single(s => s.Day == Day.Monday).TeacherId.Should().Be(chico.Id);
        updated.Schedules.Single(s => s.Day == Day.Wednesday).TeacherId.Should().Be(ana.Id);
    }

    [Test]
    public async Task Classes_UpdateClassTeachers_Should_not_auto_fix_an_orphan_schedule_when_reassigning_the_same_two_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var joao = await client.CreateTeacher("João Alves", DataGen.Email).Success();
        await client.AssignTeachersToDiscipline(discipline.Id, [chico.Id, ana.Id, joao.Id]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, ana.Id]);
        await client.UpdateClassSchedules(@class.Id,
        [
            (Day.Monday, Hour.H07_00, Hour.H10_00, chico.Id, null),
            (Day.Wednesday, Hour.H07_00, Hour.H10_00, ana.Id, null),
        ]);

        await client.UpdateClassTeachers(@class.Id, [chico.Id, joao.Id]);

        // Act
        var result = await client.UpdateClassTeachers(@class.Id, [chico.Id, joao.Id]);

        // Assert
        result.ShouldBeSuccess();

        var updated = await client.GetClass(@class.Id).Success();
        updated.Teachers.Select(t => t.Id).Should().BeEquivalentTo([chico.Id, joao.Id]);
        updated.Schedules.Single(s => s.Day == Day.Monday).TeacherId.Should().Be(chico.Id);
        updated.Schedules.Single(s => s.Day == Day.Wednesday).TeacherId.Should().BeNull();
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClass(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetTeacherClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_when_teacher_is_not_assigned_to_it()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClass(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_GetTeacherClass_Should_not_get_class_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(otherTeacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClass(@class.Id);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherClass_Should_get_class_details()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(@class.Id);
        details.Discipline.Should().Be("Geometria");
        details.Period.Should().Be(period.Name);
        details.Vacancies.Should().Be(40);
        details.Status.Should().Be(ClassStatus.OnPreEnrollment);
        details.Schedules.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherClass_Should_get_class_schedules_ordered_by_day_and_time()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacherName = DataGen.UserName;
        var teacher = await director.CreateTeacher(teacherName, DataGen.Email).Success();

        var campus = await director.CreateCampus().Success();
        var classroom = await director.CreateClassroom(campus.Id, name: "Sala 05").Success();

        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        await director.UpdateClassSchedules(@class.Id,
        [
            (Day.Wednesday, Hour.H07_00, Hour.H09_00, null, classroom.Id),
            (Day.Monday, Hour.H19_00, Hour.H21_00, null, null),
            (Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, classroom.Id),
        ]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Schedules.Should().HaveCount(3);

        details.Schedules[0].Day.Should().Be(Day.Monday);
        details.Schedules[0].StartAt.Should().Be(Hour.H07_00);
        details.Schedules[0].EndAt.Should().Be(Hour.H10_00);
        details.Schedules[0].Teacher.Should().Be(teacherName);
        details.Schedules[0].Classroom.Should().Be("Sala 05");

        details.Schedules[1].Day.Should().Be(Day.Monday);
        details.Schedules[1].StartAt.Should().Be(Hour.H19_00);
        details.Schedules[1].Teacher.Should().BeNull();
        details.Schedules[1].Classroom.Should().BeNull();

        details.Schedules[2].Day.Should().Be(Day.Wednesday);
        details.Schedules[2].Teacher.Should().BeNull();
        details.Schedules[2].Classroom.Should().Be("Sala 05");
    }

    #endregion
}

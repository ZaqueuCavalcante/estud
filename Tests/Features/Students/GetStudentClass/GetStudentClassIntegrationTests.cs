namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_user_is_not_a_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentClass(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_class_not_found()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_of_another_institution()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        var discipline = await otherDirector.CreateDiscipline().Success();
        var period = await otherDirector.ShortcutGetFirstAcademicPeriod();
        var @class = await otherDirector.CreateClass(discipline.Id, period.Id).Success();

        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentClass_Should_not_get_class_when_student_is_not_enrolled_in_it()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var discipline = await director.CreateDiscipline().Success();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        result.ShouldBeError(StudentNotEnrolledInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentClass_Should_get_class_details()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.StudentEmail);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(@class.Id);
        details.Period.Should().Be(period.Name);
        details.Discipline.Should().Be("Geometria");
        details.Status.Should().Be(ClassStatus.Started);
        details.MyStatus.Should().Be(StudentClassStatus.Matriculado);
        details.Schedules.Should().ContainSingle();
    }

    [Test]
    public async Task Students_GetStudentClass_Should_get_class_schedules_with_classroom_ordered_by_day_and_time()
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

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, @class.Id);

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Teachers.Should().Equal(teacherName);
        details.Schedules.Should().HaveCount(3);

        details.Schedules[0].Day.Should().Be(Day.Monday);
        details.Schedules[0].StartAt.Should().Be(Hour.H07_00);
        details.Schedules[0].EndAt.Should().Be(Hour.H10_00);
        details.Schedules[0].TeacherId.Should().Be(teacher.Id);
        details.Schedules[0].Teacher.Should().Be(teacherName);
        details.Schedules[0].ClassroomId.Should().Be(classroom.Id);
        details.Schedules[0].Classroom.Should().Be("Sala 05");

        details.Schedules[1].Day.Should().Be(Day.Monday);
        details.Schedules[1].StartAt.Should().Be(Hour.H19_00);
        details.Schedules[1].TeacherId.Should().BeNull();
        details.Schedules[1].Teacher.Should().BeNull();
        details.Schedules[1].ClassroomId.Should().BeNull();
        details.Schedules[1].Classroom.Should().BeNull();

        details.Schedules[2].Day.Should().Be(Day.Wednesday);
        details.Schedules[2].Teacher.Should().BeNull();
        details.Schedules[2].Classroom.Should().Be("Sala 05");
    }

    #endregion
}

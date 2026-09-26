namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_GetTeacherHome_Should_not_get_home_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_GetTeacherHome_Should_not_get_home_when_user_is_a_director()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_GetTeacherHome_Should_not_get_home_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_GetTeacherHome_Should_get_empty_home_when_teacher_has_no_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.ShortcutCreateStartedClass();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        var home = result.Success;
        home.ActiveClasses.Should().Be(0);
        home.Students.Should().Be(0);
        home.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherHome_Should_get_not_finalized_classes_with_started_ones_first()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var quimica = await director.CreateDiscipline("Química").Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [algebra.Id, quimica.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();

        var algebraClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(algebraClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(algebraClass.Id, [(Day.Tuesday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(algebraClass.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await director.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2)).Success();

        var quimicaClass = await director.CreateClass(quimica.Id, period.Id).Success();
        await director.UpdateClassTeachers(quimicaClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(quimicaClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await director.ReleaseClassForEnrollment(quimicaClass.Id);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(student.Id, quimicaClass.Id);
        await director.StartClass(quimicaClass.Id);

        var client = await _back.LoginAs(teacher.Email);
        var lessons = await client.GetTeacherClassLessons(quimicaClass.Id).Success();

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        var classes = result.Success.Classes;
        classes.Should().HaveCount(2);

        classes[0].Id.Should().Be(quimicaClass.Id);
        classes[0].Discipline.Should().Be("Química");
        classes[0].Period.Should().Be(period.Name);
        classes[0].Status.Should().Be(ClassStatus.Started);
        classes[0].Students.Should().Be(1);
        classes[0].Lessons.Should().Be(lessons.Lessons.Count);
        classes[0].FinishedLessons.Should().Be(0);

        classes[1].Id.Should().Be(algebraClass.Id);
        classes[1].Discipline.Should().Be("Álgebra");
        classes[1].Status.Should().Be(ClassStatus.OnEnrollment);
        classes[1].Students.Should().Be(0);
        classes[1].Lessons.Should().Be(0);
    }

    [Test]
    public async Task Teachers_GetTeacherHome_Should_not_get_pre_enrollment_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var algebra = await director.CreateDiscipline("Álgebra").Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [algebra.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var algebraClass = await director.CreateClass(algebra.Id, period.Id).Success();
        await director.UpdateClassTeachers(algebraClass.Id, [teacher.Id]);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.Success.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_GetTeacherHome_Should_count_each_student_once_across_active_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var algebra = await director.CreateDiscipline("Álgebra").Success();
        var geometria = await director.CreateDiscipline("Geometria").Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [algebra.Id, geometria.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var algebraClass = await director.CreateClass(algebra.Id, period.Id).Success();
        var geometriaClass = await director.CreateClass(geometria.Id, period.Id).Success();

        await director.UpdateClassTeachers(algebraClass.Id, [teacher.Id]);
        await director.UpdateClassTeachers(geometriaClass.Id, [teacher.Id]);
        await director.UpdateClassSchedules(algebraClass.Id, [(Day.Monday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);
        await director.UpdateClassSchedules(geometriaClass.Id, [(Day.Tuesday, Hour.H07_00, Hour.H10_00, teacher.Id, null)]);

        await director.ReleaseClassForEnrollment(algebraClass.Id);
        await director.ReleaseClassForEnrollment(geometriaClass.Id);

        var ana = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var bruno = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.AssignStudentToClass(ana.Id, algebraClass.Id);
        await director.AssignStudentToClass(ana.Id, geometriaClass.Id);
        await director.AssignStudentToClass(bruno.Id, geometriaClass.Id);

        await director.StartClass(algebraClass.Id);
        await director.StartClass(geometriaClass.Id);

        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.Success.ActiveClasses.Should().Be(2);
        result.Success.Students.Should().Be(2);
    }

    [Test]
    public async Task Teachers_GetTeacherHome_Should_count_lessons_with_attendance_as_finished()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var client = await _back.LoginAs(@class.TeacherEmail);
        var lessonIds = await client.ShortcutGetClassLessons(@class.Id);

        await client.CreateLessonAttendance(lessonIds[0], @class.StudentIds);

        // Act
        var result = await client.GetTeacherHome();

        // Assert
        result.Success.Classes.Single().FinishedLessons.Should().Be(1);
    }

    #endregion
}

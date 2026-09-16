namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateLessonPlan(lessonId: 1, plannedContent: "Introdução a grafos.");

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateLessonPlan(lessonId: 1, plannedContent: "Introdução a grafos.");

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.UpdateLessonPlan(lessonId: 1, plannedContent: "Introdução a grafos.");

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_when_lesson_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateLessonPlan(lessonId: 999999, plannedContent: "Introdução a grafos.");

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_on_lesson_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var otherTeacher = await _back.LoggedAsTeacher();

        // Act
        var result = await otherTeacher.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_on_lesson_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_not_update_plan_when_content_is_too_long()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.First(), new string('a', 10001));

        // Assert
        result.ShouldBeError(InvalidClassLessonPlan.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_update_plan_of_future_lesson()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.ShortcutGetLastAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(students: [], periodId: period.Id);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.Last(), "Introdução a grafos.");

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.Last());
        lesson.PlannedContent.Should().Be("Introdução a grafos.");
        lesson.Status.Should().Be(ClassLessonStatus.Pending);
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_update_an_existing_plan()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.First(), "  Busca em largura e em profundidade.  ");

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.PlannedContent.Should().Be("Busca em largura e em profundidade.");
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_clear_plan_when_content_is_empty()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.First(), "");

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.PlannedContent.Should().BeNull();
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_clear_plan_when_content_has_only_spaces()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.First(), "   ");

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.PlannedContent.Should().BeNull();
    }

    [Test]
    public async Task Teachers_UpdateLessonPlan_Should_update_plan_of_lesson_with_attendance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var students = @class.StudentIds;

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.ShortcutGetClassLessons(@class.Id);
        await teacherClient.CreateLessonAttendance(lessons.First(), [students[0]]);

        // Act
        var result = await teacherClient.UpdateLessonPlan(lessons.First(), "Introdução a grafos.");

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.PlannedContent.Should().Be("Introdução a grafos.");
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.PresentStudents.Should().BeEquivalentTo([students[0]]);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.CreateLessonAttendance(lessonId: 1, presentStudents: []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateLessonAttendance(lessonId: 1, presentStudents: []);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_lesson_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.CreateLessonAttendance(lessonId: 999999, presentStudents: []);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_on_lesson_of_another_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        var otherTeacher = await _back.LoggedAsTeacher();

        // Act
        var result = await otherTeacher.CreateLessonAttendance(lessons.First(), presentStudents: []);

        // Assert
        result.ShouldBeError(ClassLessonNotFound.I);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_on_lesson_of_another_teacher()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var otherTeacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        var client = await _back.LoginAs(otherTeacher.Email);

        // Act
        var result = await client.CreateLessonAttendance(lessons.First(), presentStudents: []);

        // Assert
        result.ShouldBeError(TeacherNotAssignedToClass.I);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_lesson_is_in_the_future()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.GetLastAcademicPeriod();
        var @class = await director.ShortcutCreateStartedClass(students: [], periodId: period.Id);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.Last(), presentStudents: []);

        // Assert
        result.ShouldBeError(ClassLessonNotStarted.I);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_student_is_not_enrolled_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), [999999]);

        // Assert
        result.ShouldBeError(InvalidStudentsList.I);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_not_create_attendance_when_student_is_duplicated()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var student = @class.StudentIds[0];

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), [student, student]);

        // Assert
        result.ShouldBeError(InvalidStudentsList.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_create_attendance()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var students = @class.StudentIds;

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), [students[0]]);

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.PresentStudents.Should().BeEquivalentTo([students[0]]);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_create_attendance_when_lesson_is_today()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();
        var student = @class.StudentIds[0];

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Nenhum endpoint agenda uma aula para hoje: a data sai do calendário do período
        // letivo, e hoje pode nem ser dia letivo. Por isso a data vai direto no banco.
        await using (var arrangeCtx = _back.GetDbContext())
        {
            var lessonId = lessons.First();
            var lesson = await arrangeCtx.ClassLessons.FirstAsync(l => l.Id == lessonId);
            lesson.Date = DateOnly.FromDateTime(DateTime.UtcNow);
            await arrangeCtx.SaveChangesAsync();
        }

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), [student]);

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var finished = classLessons.Lessons.First(l => l.Id == lessons.First());
        finished.Status.Should().Be(ClassLessonStatus.Finalized);
        finished.PresentStudents.Should().BeEquivalentTo([student]);
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_create_attendance_for_class_without_students()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), []);

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.PresentStudents.Should().BeEmpty();
    }

    [Test]
    public async Task Teachers_CreateLessonAttendance_Should_update_attendance_when_lesson_is_called_again()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(studentsCount: 2);
        var students = @class.StudentIds;

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = await teacherClient.GetClassLessons(@class.Id);
        await teacherClient.CreateLessonAttendance(lessons.First(), [students[0]]);

        // Act
        var result = await teacherClient.CreateLessonAttendance(lessons.First(), [students[1]]);

        // Assert
        result.ShouldBeSuccess();

        var classLessons = await teacherClient.GetTeacherClassLessons(@class.Id).Success();
        var lesson = classLessons.Lessons.First(l => l.Id == lessons.First());
        lesson.Status.Should().Be(ClassLessonStatus.Finalized);
        lesson.PresentStudents.Should().BeEquivalentTo([students[1]]);
    }

    #endregion
}

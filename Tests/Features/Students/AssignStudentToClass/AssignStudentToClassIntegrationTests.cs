namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.AssignStudentToClass(studentId: 1, classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.AssignStudentToClass(studentId: 1, classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_student_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.AssignStudentToClass(studentId: 999999, classId: 1);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.AssignStudentToClass(student.Id, classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_student_already_enrolled()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeError(StudentAlreadyEnrolledInClass.I);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_assign_when_class_has_no_vacancies()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id, vacancies: 1).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var studentA = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var studentB = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.AssignStudentToClass(studentA.Id, @class.Id);

        // Act
        var result = await client.AssignStudentToClass(studentB.Id, @class.Id);

        // Assert
        result.ShouldBeError(NoVacanciesInClass.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_AssignStudentToClass_Should_assign_student_to_class()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var studentClass = await client.GetClass(@class.Id).Success();
        studentClass.Students.Should().ContainSingle(x => x.Id == student.Id && x.Status == StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_create_works_for_activities_already_created_in_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var activity = await teacherClient.CreateClassActivity(@class.Id).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await director.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var classActivity = await teacherClient.GetTeacherClassActivity(@class.Id, activity.Id).Success();
        classActivity.TotalWorks.Should().Be(2);
        classActivity.Works.Should().ContainSingle(w =>
            w.StudentId == student.Id &&
            w.Status == ClassActivityWorkStatus.Pending &&
            w.Entries.Count == 0 &&
            w.Value == 0
        );

        var studentClient = await _back.LoginAs(student.Email);
        var activities = await studentClient.GetStudentClassActivities(@class.Id).Success();
        activities.Activities.Should().ContainSingle(a => a.Id == activity.Id && a.WorkStatus == ClassActivityWorkStatus.Pending);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_create_works_for_all_activities_of_the_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var firstActivity = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var secondActivity = await teacherClient.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 60).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await director.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        foreach (var activityId in new[] { firstActivity.Id, secondActivity.Id })
        {
            var classActivity = await teacherClient.GetTeacherClassActivity(@class.Id, activityId).Success();
            classActivity.Works.Should().ContainSingle(w => w.StudentId == student.Id);
        }
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_create_absences_for_lessons_already_called()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = (await teacherClient.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        var calledLesson = lessons.First();
        await teacherClient.CreateLessonAttendance(calledLesson.Id, @class.StudentIds);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await director.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var lesson = await teacherClient.GetTeacherClassLesson(@class.Id, calledLesson.Id).Success();
        lesson.Students.Should().ContainSingle(s => s.Id == student.Id && !s.Present);

        var studentClient = await _back.LoginAs(student.Email);
        var calendar = await studentClient.GetStudentAttendanceCalendar(calledLesson.Date.Year).Success();
        var day = calendar.Items.Single(i => i.Date == calledLesson.Date.ToDateTime(TimeOnly.MinValue));
        day.Status.Should().Be(StudentDayAttendanceStatus.Absent);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_not_create_absences_for_lessons_not_called_yet()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);
        var lessons = (await teacherClient.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        var calledLesson = lessons.First();
        var pendingLesson = lessons.Last();
        await teacherClient.CreateLessonAttendance(calledLesson.Id, @class.StudentIds);

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await director.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var studentClient = await _back.LoginAs(student.Email);
        var calendar = await studentClient.GetStudentAttendanceCalendar(pendingLesson.Date.Year).Success();
        var day = calendar.Items.Single(i => i.Date == pendingLesson.Date.ToDateTime(TimeOnly.MinValue));
        day.Status.Should().Be(StudentDayAttendanceStatus.Undefined);
    }

    [Test]
    public async Task Students_AssignStudentToClass_Should_assign_student_to_class_without_activities_and_lessons_called()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var @class = await director.ShortcutCreateStartedClass(students: []);
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var teacherClient = await _back.LoginAs(@class.TeacherEmail);

        // Act
        var result = await director.AssignStudentToClass(student.Id, @class.Id);

        // Assert
        result.ShouldBeSuccess();

        var students = await teacherClient.GetTeacherClassStudents(@class.Id).Success();
        students.Students.Should().ContainSingle(s => s.Id == student.Id && s.AverageAttendance == 0 && s.AverageGrade == 0);
    }

    #endregion
}

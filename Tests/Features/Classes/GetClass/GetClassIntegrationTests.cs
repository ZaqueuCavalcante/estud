namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetClass(classId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Classes_GetClass_Should_not_get_class_when_class_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetClass(classId: 999999);

        // Assert
        result.ShouldBeError(ClassNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Classes_GetClass_Should_get_class_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(@class.Id);
        details.Discipline.Should().Be("Geometria");
        details.Period.Should().Be(period.Name);
        details.Status.Should().Be(ClassStatus.OnPreEnrollment);
        details.Schedules.Should().BeEmpty();
        details.Students.Should().BeEmpty();
    }

    [Test]
    public async Task Classes_GetClass_Should_get_class_with_enrolled_students()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2));
        await client.ReleaseClassForEnrollment(@class.Id);

        await client.AssignStudentToClass(student.Id, @class.Id);

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Status.Should().Be(ClassStatus.OnEnrollment);
        details.Students.Should().ContainSingle();
        details.Students[0].Id.Should().Be(student.Id);
        details.Students[0].Status.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_class_as_awaiting_start_when_enrollment_period_is_finalized()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var enrollmentPeriod = await client.CreateEnrollmentPeriod(startAt: today.AddDays(-2), endAt: today.AddDays(2)).Success();
        await client.ReleaseClassForEnrollment(@class.Id);

        await client.UpdateEnrollmentPeriod(enrollmentPeriod.Id, startAt: today.AddDays(-2), endAt: today.AddDays(-1));

        // Act
        var result = await client.GetClass(@class.Id);

        // Assert
        result.Success.Status.Should().Be(ClassStatus.OnReview);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_students_and_class_attendances()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id]);
        await teacher.CreateLessonAttendance(lessons[2].Id, []);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.First(s => s.Id == ana.Id).AverageAttendance.Should().Be(66.7M);
        details.Students.First(s => s.Id == bruno.Id).AverageAttendance.Should().Be(33.3M);
        details.AverageAttendance.Should().Be(50M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_full_attendances_when_every_student_was_present()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var lessons = (await teacher.GetTeacherClassLessons(@class.Id).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id, bruno.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageAttendance.Should().Be(100M));
        details.AverageAttendance.Should().Be(100M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageAttendance.Should().Be(0M));
        details.AverageAttendance.Should().Be(0M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_partial_average_grade_when_only_one_activity_was_graded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, student.Id, 9M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2.3M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_a_different_average_grade_for_each_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var activity = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, ana.Id, 8M);
        await teacher.AddStudentActivityNote(@class.Id, activity.Id, bruno.Id, 5M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        var details = result.Success;
        details.Students.First(s => s.Id == ana.Id).AverageGrade.Should().Be(4.0M);
        details.Students.First(s => s.Id == bruno.Id).AverageGrade.Should().Be(2.5M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_average_grade_from_the_two_highest_notes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 100).Success();
        var n2 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100).Success();
        var n3 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N3, weight: 100).Success();
        await teacher.AddStudentActivityNote(@class.Id, n1.Id, student.Id, 9M);
        await teacher.AddStudentActivityNote(@class.Id, n2.Id, student.Id, 4M);
        await teacher.AddStudentActivityNote(@class.Id, n3.Id, student.Id, 7M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(8M);
    }

    [Test]
    public async Task Classes_GetClass_Should_add_up_the_weights_of_the_activities_of_the_same_note_type()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var first = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 40).Success();
        var second = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 60).Success();
        await teacher.AddStudentActivityNote(@class.Id, first.Id, student.Id, 10M);
        await teacher.AddStudentActivityNote(@class.Id, second.Id, student.Id, 5M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(3.5M);
    }

    [Test]
    public async Task Classes_GetClass_Should_count_an_uncorrected_activity_as_zero()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([student.Id]);
        var teacher = await _back.LoginAs(@class.TeacherEmail);

        var n1 = await teacher.CreateClassActivity(@class.Id, ClassNoteType.N1, weight: 50).Success();
        await teacher.CreateClassActivity(@class.Id, ClassNoteType.N2, weight: 100);
        await teacher.AddStudentActivityNote(@class.Id, n1.Id, student.Id, 8M);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Single().AverageGrade.Should().Be(2M);
    }

    [Test]
    public async Task Classes_GetClass_Should_get_zeroed_average_grades_when_the_class_has_no_activity()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var @class = await director.ShortcutCreateStartedClass([ana.Id, bruno.Id]);

        // Act
        var result = await director.GetClass(@class.Id);

        // Assert
        result.Success.Students.Should().AllSatisfy(s => s.AverageGrade.Should().Be(0M));
    }

    #endregion
}

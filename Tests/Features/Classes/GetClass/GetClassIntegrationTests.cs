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
        var result = await client.GetClass(1);

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
        var result = await client.GetClass(1);

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
        var result = await client.GetClass(999999);

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
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.GetFirstAcademicPeriod();
        var @class = await client.CreateClass(discipline.Id, period.Id).Success();

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
        var period = await director.CreateAcademicPeriod().Success();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var (classId, teacher) = await _back.ArrangeStartedClass(director, period.Id, [ana.Id, bruno.Id]);

        var lessons = (await teacher.GetTeacherClassLessons(classId).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id]);
        await teacher.CreateLessonAttendance(lessons[2].Id, []);

        // Act
        var result = await director.GetClass(classId);

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
        var period = await director.CreateAcademicPeriod().Success();
        var ana = await director.CreateStudent("Ana Beatriz", DataGen.Email).Success();
        var bruno = await director.CreateStudent("Bruno Silva", DataGen.Email).Success();
        var (classId, teacher) = await _back.ArrangeStartedClass(director, period.Id, [ana.Id, bruno.Id]);

        var lessons = (await teacher.GetTeacherClassLessons(classId).Success()).Lessons;
        await teacher.CreateLessonAttendance(lessons[0].Id, [ana.Id, bruno.Id]);
        await teacher.CreateLessonAttendance(lessons[1].Id, [ana.Id, bruno.Id]);

        // Act
        var result = await director.GetClass(classId);

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
        var period = await director.GetFirstAcademicPeriod();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var (classId, _) = await _back.ArrangeStartedClass(director, period.Id, [student.Id]);

        // Act
        var result = await director.GetClass(classId);

        // Assert
        var details = result.Success;
        details.Students.Should().AllSatisfy(s => s.AverageAttendance.Should().Be(0M));
        details.AverageAttendance.Should().Be(0M);
    }

    #endregion
}

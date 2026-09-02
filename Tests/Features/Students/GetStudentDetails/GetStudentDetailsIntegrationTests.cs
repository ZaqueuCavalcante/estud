namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetStudentDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetStudentDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_user_is_a_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_when_student_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentDetails(999999);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_not_get_student_details_of_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        var student = await otherClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var email = DataGen.Email;
        var student = await client.CreateStudent("Ana Lima", email).Success();

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(student.Id);
        details.Name.Should().Be("Ana Lima");
        details.Email.Should().Be(email);
        details.EnrollmentCode.Should().NotBeEmpty();
        details.Status.Should().Be(StudentStatus.Enrolled);
        details.Course.Should().BeNull();
        details.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details_with_current_course_offering()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.GetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();
        await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Course.Should().NotBeNull();
        details.Course!.CourseOfferingId.Should().Be(offering.Id);
        details.Course.Period.Should().Be(period.Name);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details_with_classes()
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
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().ContainSingle();
        details.Classes[0].Id.Should().Be(@class.Id);
        details.Classes[0].Discipline.Should().Be("Geometria");
        details.Classes[0].Period.Should().Be(period.Name);
        details.Classes[0].Status.Should().Be(ClassStatus.OnEnrollment);
        details.Classes[0].MyStatus.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_attendances_of_each_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometry = await director.ShortcutCreateStartedClass([student.Id]);
        var algebra = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var geometryTeacher = await _back.LoginAs(geometry.TeacherEmail);
        var algebraTeacher = await _back.LoginAs(algebra.TeacherEmail);

        var geometryLessons = (await geometryTeacher.GetTeacherClassLessons(geometry.Id).Success()).Lessons;
        await geometryTeacher.CreateLessonAttendance(geometryLessons[0].Id, [student.Id]);
        await geometryTeacher.CreateLessonAttendance(geometryLessons[1].Id, [student.Id]);
        await geometryTeacher.CreateLessonAttendance(geometryLessons[2].Id, []);

        var algebraLessons = (await algebraTeacher.GetTeacherClassLessons(algebra.Id).Success()).Lessons;
        await algebraTeacher.CreateLessonAttendance(algebraLessons[0].Id, [student.Id]);
        await algebraTeacher.CreateLessonAttendance(algebraLessons[1].Id, []);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.Id).AverageAttendance.Should().Be(66.7M);
        details.Classes.First(c => c.Id == algebra.Id).AverageAttendance.Should().Be(50M);
        details.AverageAttendance.Should().Be(60M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_attendance_only_of_started_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var started = await director.ShortcutCreateStartedClass([student.Id]);
        var finalized = await director.ShortcutCreateStartedClass([student.Id], "Álgebra", Day.Tuesday);

        var startedTeacher = await _back.LoginAs(started.TeacherEmail);
        var finalizedTeacher = await _back.LoginAs(finalized.TeacherEmail);

        var startedLessons = (await startedTeacher.GetTeacherClassLessons(started.Id).Success()).Lessons;
        await startedTeacher.CreateLessonAttendance(startedLessons[0].Id, [student.Id]);
        await startedTeacher.CreateLessonAttendance(startedLessons[1].Id, []);

        var finalizedLessons = (await finalizedTeacher.GetTeacherClassLessons(finalized.Id).Success()).Lessons;
        await finalizedTeacher.CreateLessonAttendance(finalizedLessons[0].Id, [student.Id]);
        await finalizedTeacher.CreateLessonAttendance(finalizedLessons[1].Id, [student.Id]);

        // Nenhum endpoint encerra uma turma, então o status vai direto no banco.
        await using (var arrangeCtx = _back.GetDbContext())
        {
            var @class = await arrangeCtx.Classes.FirstAsync(c => c.Id == finalized.Id);
            @class.Status = ClassStatus.Finalized;
            await arrangeCtx.SaveChangesAsync();
        }

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == started.Id).AverageAttendance.Should().Be(50M);
        details.Classes.First(c => c.Id == finalized.Id).AverageAttendance.Should().Be(100M);
        details.AverageAttendance.Should().Be(50M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        await director.ShortcutCreateStartedClass([student.Id]);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.Should().ContainSingle()
            .Which.AverageAttendance.Should().Be(0M);
        details.AverageAttendance.Should().Be(0M);
    }

    #endregion
}

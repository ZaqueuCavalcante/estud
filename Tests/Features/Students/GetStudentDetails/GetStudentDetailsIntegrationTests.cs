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
        var directorClient = await _back.LoggedAsDirector();
        var email = DataGen.Email;
        var student = await directorClient.CreateStudent(DataGen.UserName, email).Success();
        var client = await _back.LoginAs(email);

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
        var period = await client.CreateAcademicPeriod().Success();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();
        await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Act
        var result = await client.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Course.Should().NotBeNull();
        details.Course!.CourseOfferingId.Should().Be(offering.Id);
        details.Course.Period.Should().Be("2024.1");
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_student_details_with_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var discipline = await client.CreateDiscipline().Success();
        var period = await client.CreateAcademicPeriod().Success();
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
        details.Classes[0].Period.Should().Be("2024.1");
        details.Classes[0].Status.Should().Be(ClassStatus.OnEnrollment);
        details.Classes[0].MyStatus.Should().Be(StudentClassStatus.Matriculado);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_attendances_of_each_class()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.CreateAcademicPeriod().Success();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var geometry = await _back.ArrangeStartedClass(director, period.Id, [student.Id], "Geometria");
        var algebra = await _back.ArrangeStartedClass(director, period.Id, [student.Id], "Álgebra", Day.Tuesday);

        var geometryLessons = (await geometry.Teacher.GetTeacherClassLessons(geometry.ClassId).Success()).Lessons;
        await geometry.Teacher.CreateLessonAttendance(geometryLessons[0].Id, [student.Id]);
        await geometry.Teacher.CreateLessonAttendance(geometryLessons[1].Id, [student.Id]);
        await geometry.Teacher.CreateLessonAttendance(geometryLessons[2].Id, []);

        var algebraLessons = (await algebra.Teacher.GetTeacherClassLessons(algebra.ClassId).Success()).Lessons;
        await algebra.Teacher.CreateLessonAttendance(algebraLessons[0].Id, [student.Id]);
        await algebra.Teacher.CreateLessonAttendance(algebraLessons[1].Id, []);

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == geometry.ClassId).AverageAttendance.Should().Be(66.7M);
        details.Classes.First(c => c.Id == algebra.ClassId).AverageAttendance.Should().Be(50M);
        details.AverageAttendance.Should().Be(60M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_average_attendance_only_of_started_classes()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.CreateAcademicPeriod().Success();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var started = await _back.ArrangeStartedClass(director, period.Id, [student.Id], "Geometria");
        var finalized = await _back.ArrangeStartedClass(director, period.Id, [student.Id], "Álgebra", Day.Tuesday);

        var startedLessons = (await started.Teacher.GetTeacherClassLessons(started.ClassId).Success()).Lessons;
        await started.Teacher.CreateLessonAttendance(startedLessons[0].Id, [student.Id]);
        await started.Teacher.CreateLessonAttendance(startedLessons[1].Id, []);

        var finalizedLessons = (await finalized.Teacher.GetTeacherClassLessons(finalized.ClassId).Success()).Lessons;
        await finalized.Teacher.CreateLessonAttendance(finalizedLessons[0].Id, [student.Id]);
        await finalized.Teacher.CreateLessonAttendance(finalizedLessons[1].Id, [student.Id]);

        // Nenhum endpoint encerra uma turma, então o status vai direto no banco.
        await using (var arrangeCtx = _back.GetDbContext())
        {
            var @class = await arrangeCtx.Classes.FirstAsync(c => c.Id == finalized.ClassId);
            @class.Status = ClassStatus.Finalized;
            await arrangeCtx.SaveChangesAsync();
        }

        // Act
        var result = await director.GetStudentDetails(student.Id);

        // Assert
        var details = result.Success;
        details.Classes.First(c => c.Id == started.ClassId).AverageAttendance.Should().Be(50M);
        details.Classes.First(c => c.Id == finalized.ClassId).AverageAttendance.Should().Be(100M);
        details.AverageAttendance.Should().Be(50M);
    }

    [Test]
    public async Task Students_GetStudentDetails_Should_get_zeroed_attendances_when_no_lesson_was_recorded()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var period = await director.CreateAcademicPeriod().Success();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        await _back.ArrangeStartedClass(director, period.Id, [student.Id]);

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

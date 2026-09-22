namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Cross_GetHomeStats_Should_not_get_home_stats_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Cross_GetHomeStats_Should_not_get_home_stats_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_not_get_home_stats_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Cross_GetHomeStats_Should_get_zeroed_home_stats_when_institution_has_no_data()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        var stats = result.Success;
        stats.EnrolledStudents.Should().Be(0);
        stats.ActiveTeachers.Should().Be(0);
        stats.OfferedCourses.Should().Be(0);
        stats.RegisteredDisciplines.Should().Be(0);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_count_registered_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateDiscipline("Cálculo I").Success();
        await client.CreateDiscipline("Álgebra Linear").Success();
        await client.CreateDiscipline("Banco de Dados").Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.RegisteredDisciplines.Should().Be(3);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_count_active_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await client.CreateTeacher(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.ActiveTeachers.Should().Be(2);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_count_each_offering_of_the_same_course()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();
        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Evening).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.OfferedCourses.Should().Be(2);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_count_enrolled_students()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var maria = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var joao = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.EnrollStudentInCourseOffering(maria.Id, offering.Id).Success();
        await client.EnrollStudentInCourseOffering(joao.Id, offering.Id).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.EnrolledStudents.Should().Be(2);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_not_count_students_without_enrollment()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.EnrolledStudents.Should().Be(0);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_count_the_same_student_once_per_enrollment()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        var morning = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();
        var evening = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Evening).Success();

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.EnrollStudentInCourseOffering(student.Id, morning.Id).Success();
        await client.EnrollStudentInCourseOffering(student.Id, evening.Id).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.EnrolledStudents.Should().Be(2);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_not_count_students_who_left_the_course()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var maria = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var joao = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.EnrollStudentInCourseOffering(maria.Id, offering.Id).Success();
        var joaoEnrollment = await client.EnrollStudentInCourseOffering(joao.Id, offering.Id).Success();

        var enrollmentId = joaoEnrollment.Id;
        await using var ctx = _back.GetDbContext();
        var enrollment = await ctx.StudentCourseEnrollments.FirstAsync(e => e.Id == enrollmentId);
        enrollment.LeftAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        result.Success.EnrolledStudents.Should().Be(1);
    }

    [Test]
    public async Task Cross_GetHomeStats_Should_not_count_data_from_other_institutions()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        await client.CreateDiscipline("Cálculo I").Success();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();
        var otherCourse = await otherClient.CreateCourse().Success();
        var otherCurriculum = await otherClient.CreateCourseCurriculum(otherCourse.Id).Success();
        var otherPeriod = await otherClient.ShortcutGetFirstAcademicPeriod();
        var otherOffering = await otherClient
            .CreateCourseOffering(otherCampus.Id, otherCourse.Id, otherCurriculum.Id, otherPeriod.Id)
            .Success();

        await otherClient.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        await otherClient.CreateDiscipline("Geometria").Success();
        var otherStudent = await otherClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await otherClient.EnrollStudentInCourseOffering(otherStudent.Id, otherOffering.Id).Success();

        // Act
        var result = await client.GetHomeStats();

        // Assert
        var stats = result.Success;
        stats.EnrolledStudents.Should().Be(0);
        stats.ActiveTeachers.Should().Be(0);
        stats.OfferedCourses.Should().Be(0);
        stats.RegisteredDisciplines.Should().Be(1);
    }

    #endregion
}

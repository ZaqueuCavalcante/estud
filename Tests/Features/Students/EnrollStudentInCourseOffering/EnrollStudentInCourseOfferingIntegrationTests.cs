namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.EnrollStudentInCourseOffering(1, 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.EnrollStudentInCourseOffering(studentId: 1, courseOfferingId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_when_student_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(studentId:999999, offering.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_when_course_offering_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, courseOfferingId: 999999);

        // Assert
        result.ShouldBeError(CourseOfferingNotFound.I);
    }

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_when_student_already_enrolled()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Assert
        result.ShouldBeError(StudentAlreadyEnrolledInCourseOffering.I);
    }

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_change_the_course_offering_of_an_enrolled_student()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var morning = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning).Success();
        var evening = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Evening).Success();

        await client.EnrollStudentInCourseOffering(student.Id, morning.Id).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, evening.Id);

        // Assert
        result.ShouldBeError(StudentAlreadyEnrolledInAnotherCourseOffering.I);

        var details = await client.GetStudent(student.Id).Success();
        details.CurrentCourseOfferingId.Should().Be(morning.Id);
    }

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_student_from_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        var student = await otherClient.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Assert
        result.ShouldBeError(StudentNotFound.I);
    }

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_not_enroll_in_course_offering_from_another_institution()
    {
        // Arrange
        var otherClient = await _back.LoggedAsDirector();
        var campus = await otherClient.CreateCampus().Success();
        var course = await otherClient.CreateCourse().Success();
        var curriculum = await otherClient.CreateCourseCurriculum(course.Id).Success();
        var period = await otherClient.ShortcutGetFirstAcademicPeriod();
        var offering = await otherClient.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Assert
        result.ShouldBeError(CourseOfferingNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_EnrollStudentInCourseOffering_Should_enroll_student_in_course_offering()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        // Act
        var result = await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Assert
        result.Success.Id.Should().BePositive();
    }

    #endregion
}

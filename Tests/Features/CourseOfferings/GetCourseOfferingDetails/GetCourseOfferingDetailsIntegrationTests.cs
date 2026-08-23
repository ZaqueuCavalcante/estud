using Estud.Back.Features.CourseCurriculums.CreateCourseCurriculum;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_not_get_course_offering_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCourseOfferingDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_not_get_course_offering_details_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCourseOfferingDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_not_get_course_offering_details_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCourseOfferingDetails(99999);

        // Assert
        result.ShouldBeError(CourseOfferingNotFound.I);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_not_get_other_institution_course_offering_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCampus = await otherClient.CreateCampus().Success();
        var otherCourse = await otherClient.CreateCourse().Success();
        var otherCurriculum = await otherClient.CreateCourseCurriculum(otherCourse.Id).Success();
        var otherPeriod = await otherClient.CreateAcademicPeriod("2024.1").Success();
        var otherOffering = await otherClient
            .CreateCourseOffering(otherCampus.Id, otherCourse.Id, otherCurriculum.Id, otherPeriod.Id)
            .Success();

        // Act
        var result = await client.GetCourseOfferingDetails(otherOffering.Id);

        // Assert
        result.ShouldBeError(CourseOfferingNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_get_course_offering_details_without_students()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus("Agreste I").Success();
        var course = await client.CreateCourse("Análise e Desenvolvimento de Sistemas", CourseType.Tecnologo).Success();
        var calculo = await client.CreateDiscipline("Cálculo I").Success();
        await client.AddCourseDisciplines(course.Id, [calculo.Id]);

        List<CreateCourseCurriculumDisciplineIn> disciplines = [new(calculo.Id, 1, 4, 60)];
        var curriculum = await client.CreateCourseCurriculum(course.Id, "Grade 2024", disciplines).Success();

        var period = await client.CreateAcademicPeriod("2024.1").Success();
        var offering = await client
            .CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning)
            .Success();

        // Act
        var result = await client.GetCourseOfferingDetails(offering.Id);

        // Assert
        var output = result.Success;
        output.Id.Should().Be(offering.Id);
        output.CampusId.Should().Be(campus.Id);
        output.Campus.Should().Be("Agreste I");
        output.CourseId.Should().Be(course.Id);
        output.Course.Should().Be("Análise e Desenvolvimento de Sistemas");
        output.CourseType.Should().Be("Tecnólogo");
        output.CourseCurriculumId.Should().Be(curriculum.Id);
        output.Curriculum.Should().Be("Grade 2024");
        output.Period.Should().Be("2024.1");
        output.Session.Should().Be(CourseSession.Morning);
        output.Disciplines.Should().Be(1);
        output.Students.Should().BeEmpty();
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferingDetails_Should_get_course_offering_details_with_students()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id, "Grade 2024").Success();
        var period = await client.CreateAcademicPeriod("2024.1").Success();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var maria = await client.CreateStudent("Maria Souza", DataGen.Email).Success();
        var joao = await client.CreateStudent("João Lima", DataGen.Email).Success();
        await client.EnrollStudentInCourseOffering(maria.Id, offering.Id);
        await client.EnrollStudentInCourseOffering(joao.Id, offering.Id);

        // Act
        var result = await client.GetCourseOfferingDetails(offering.Id);

        // Assert
        var output = result.Success;
        output.Students.Should().HaveCount(2);
        output.Students.First().Id.Should().Be(joao.Id);
        output.Students.First().Name.Should().Be("João Lima");
        output.Students.First().EnrollmentCode.Should().NotBeEmpty();
        output.Students.First().Status.Should().Be(StudentStatus.Enrolled);
        output.Students.First().EnrolledAt.Should().NotBe(default);
        output.Students.Last().Name.Should().Be("Maria Souza");
    }

    #endregion
}

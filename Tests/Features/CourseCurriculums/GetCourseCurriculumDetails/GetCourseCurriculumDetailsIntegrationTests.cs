using Estud.Back.Features.CourseCurriculums.CreateCourseCurriculum;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_not_get_course_curriculum_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCourseCurriculumDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_not_get_course_curriculum_details_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCourseCurriculumDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_not_get_course_curriculum_details_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCourseCurriculumDetails(99999);

        // Assert
        result.ShouldBeError(CourseCurriculumNotFound.I);
    }

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_not_get_other_institution_course_curriculum_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCourse = await otherClient.CreateCourse().Success();
        var otherCurriculum = await otherClient.CreateCourseCurriculum(otherCourse.Id).Success();

        // Act
        var result = await client.GetCourseCurriculumDetails(otherCurriculum.Id);

        // Assert
        result.ShouldBeError(CourseCurriculumNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_get_course_curriculum_details_without_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse("Análise e Desenvolvimento de Sistemas", CourseType.Tecnologo).Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id, "Grade 2024").Success();

        // Act
        var result = await client.GetCourseCurriculumDetails(curriculum.Id);

        // Assert
        var output = result.Success;
        output.Id.Should().Be(curriculum.Id);
        output.Name.Should().Be("Grade 2024");
        output.CourseId.Should().Be(course.Id);
        output.Course.Should().Be("Análise e Desenvolvimento de Sistemas");
        output.CourseType.Should().Be("Tecnólogo");
        output.Periods.Should().Be(0);
        output.TotalCredits.Should().Be(0);
        output.TotalWorkload.Should().Be(0);
        output.Students.Should().Be(0);
        output.Disciplines.Should().BeEmpty();
        output.Offerings.Should().BeEmpty();
    }

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_get_course_curriculum_details_with_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var calculo = await client.CreateDiscipline("Cálculo I").Success();
        var algebra = await client.CreateDiscipline("Álgebra").Success();
        await client.AssignDisciplinesToCourse(course.Id, [calculo.Id, algebra.Id]);

        List<CreateCourseCurriculumDisciplineIn> disciplines =
        [
            new(calculo.Id, 2, 4, 72),
            new(algebra.Id, 1, 6, 60),
        ];
        var curriculum = await client.CreateCourseCurriculum(course.Id, "Grade 2024", disciplines).Success();

        // Act
        var result = await client.GetCourseCurriculumDetails(curriculum.Id);

        // Assert
        var output = result.Success;
        output.Periods.Should().Be(2);
        output.TotalCredits.Should().Be(10);
        output.TotalWorkload.Should().Be(132);

        output.Disciplines.Should().HaveCount(2);
        output.Disciplines.First().Id.Should().Be(algebra.Id);
        output.Disciplines.First().Name.Should().Be("Álgebra");
        output.Disciplines.First().Code.Should().NotBeEmpty();
        output.Disciplines.First().Period.Should().Be(1);
        output.Disciplines.First().Credits.Should().Be(6);
        output.Disciplines.First().Workload.Should().Be(60);
        output.Disciplines.Last().Name.Should().Be("Cálculo I");
        output.Disciplines.Last().Period.Should().Be(2);
    }

    [Test]
    public async Task CourseCurriculums_GetCourseCurriculumDetails_Should_get_course_curriculum_details_with_offerings()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id, "Grade 2024").Success();
        var period = await client.GetFirstAcademicPeriod();
        var offering = await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var student = await client.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await client.EnrollStudentInCourseOffering(student.Id, offering.Id);

        // Act
        var result = await client.GetCourseCurriculumDetails(curriculum.Id);

        // Assert
        var output = result.Success;
        output.Offerings.Should().HaveCount(1);
        output.Offerings.First().Id.Should().Be(offering.Id);
        output.Offerings.First().Period.Should().Be(period.Name);
        output.Offerings.First().Session.Should().Be(CourseSession.Evening);
        output.Offerings.First().Students.Should().Be(1);
        output.Students.Should().Be(1);
    }

    #endregion
}

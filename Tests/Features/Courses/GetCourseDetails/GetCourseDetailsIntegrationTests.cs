using Estud.Back.Features.CourseCurriculums.CreateCourseCurriculum;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Courses_GetCourseDetails_Should_not_get_course_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCourseDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Courses_GetCourseDetails_Should_not_get_course_details_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCourseDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Courses_GetCourseDetails_Should_not_get_course_details_not_found()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCourseDetails(99999);

        // Assert
        result.ShouldBeError(CourseNotFound.I);
    }

    [Test]
    public async Task Courses_GetCourseDetails_Should_not_get_other_institution_course_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherCourse = await otherClient.CreateCourse().Success();

        // Act
        var result = await client.GetCourseDetails(otherCourse.Id);

        // Assert
        result.ShouldBeError(CourseNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Courses_GetCourseDetails_Should_get_course_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse("Análise e Desenvolvimento de Sistemas", CourseType.Tecnologo).Success();

        // Act
        var result = await client.GetCourseDetails(course.Id);

        // Assert
        result.Success.Id.Should().Be(course.Id);
        result.Success.Name.Should().Be("Análise e Desenvolvimento de Sistemas");
        result.Success.Type.Should().Be("Tecnólogo");
        result.Success.TypeValue.Should().Be(CourseType.Tecnologo);
        result.Success.Students.Should().Be(0);
        result.Success.Disciplines.Should().BeEmpty();
        result.Success.Curriculums.Should().BeEmpty();
        result.Success.Offerings.Should().BeEmpty();
    }

    [Test]
    public async Task Courses_GetCourseDetails_Should_get_course_details_with_disciplines()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var calculo = await client.CreateDiscipline("Cálculo I").Success();
        var geometria = await client.CreateDiscipline("Geometria").Success();
        await client.AddCourseDisciplines(course.Id, [calculo.Id, geometria.Id]);

        // Act
        var result = await client.GetCourseDetails(course.Id);

        // Assert
        result.Success.Disciplines.Should().HaveCount(2);
        result.Success.Disciplines.First().Name.Should().Be("Cálculo I");
        result.Success.Disciplines.First().Code.Should().NotBeEmpty();
        result.Success.Disciplines.Last().Name.Should().Be("Geometria");
    }

    [Test]
    public async Task Courses_GetCourseDetails_Should_get_course_details_with_curriculums()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var course = await client.CreateCourse().Success();
        var discipline = await client.CreateDiscipline("Cálculo I").Success();
        await client.AddCourseDisciplines(course.Id, [discipline.Id]);

        List<CreateCourseCurriculumDisciplineIn> disciplines =
        [
            new(discipline.Id, 1, 4, 60),
        ];
        await client.CreateCourseCurriculum(course.Id, "Grade 2024", disciplines);

        // Act
        var result = await client.GetCourseDetails(course.Id);

        // Assert
        result.Success.Curriculums.Should().HaveCount(1);
        result.Success.Curriculums.First().Name.Should().Be("Grade 2024");
        result.Success.Curriculums.First().Disciplines.Should().Be(1);
    }

    [Test]
    public async Task Courses_GetCourseDetails_Should_get_course_details_with_offerings()
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
        var result = await client.GetCourseDetails(course.Id);

        // Assert
        result.Success.Offerings.Should().HaveCount(1);
        result.Success.Offerings.First().Id.Should().Be(offering.Id);
        result.Success.Offerings.First().Curriculum.Should().Be("Grade 2024");
        result.Success.Offerings.First().Period.Should().Be(period.Name);
        result.Success.Offerings.First().Session.Should().Be(CourseSession.Evening);
        result.Success.Offerings.First().Students.Should().Be(1);
        result.Success.Students.Should().Be(1);
    }

    #endregion
}

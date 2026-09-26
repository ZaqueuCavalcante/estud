namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_not_get_offerings_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetCourseOfferings();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_not_get_offerings_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetCourseOfferings();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_return_empty_list()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetCourseOfferings();

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(0);
        offerings.Items.Should().BeEmpty();
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_return_offerings()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id);

        // Act
        var result = await client.GetCourseOfferings();

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(1);
        offerings.Page.Should().Be(1);
        offerings.PageSize.Should().Be(10);
        offerings.Items[0].Period.Should().Be(period.Name);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_return_only_the_first_10_offerings_by_default()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        for (var i = 1; i <= 12; i++)
            await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id);

        // Act
        var result = await client.GetCourseOfferings();

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(12);
        offerings.Page.Should().Be(1);
        offerings.PageSize.Should().Be(10);
        offerings.Items.Should().HaveCount(10);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_return_offerings_from_the_second_page()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        for (var i = 1; i <= 12; i++)
            await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id);

        // Act
        var result = await client.GetCourseOfferings(page: 2);

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(12);
        offerings.Page.Should().Be(2);
        offerings.Items.Should().HaveCount(2);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_filter_offerings_by_campus()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campusA = await client.CreateCampus("Agreste I").Success();
        var campusB = await client.CreateCampus("Agreste II").Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        await client.CreateCourseOffering(campusA.Id, course.Id, curriculum.Id, period.Id);
        await client.CreateCourseOffering(campusB.Id, course.Id, curriculum.Id, period.Id);
        await client.CreateCourseOffering(campusB.Id, course.Id, curriculum.Id, period.Id);

        // Act
        var result = await client.GetCourseOfferings(campusId: campusB.Id);

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(2);
        offerings.Items.Should().OnlyContain(o => o.Campus == "Agreste II");
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_filter_offerings_by_period()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var firstPeriod = await client.ShortcutGetFirstAcademicPeriod();
        var lastPeriod = await client.ShortcutGetLastAcademicPeriod();

        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, firstPeriod.Id);
        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, lastPeriod.Id);

        // Act
        var result = await client.GetCourseOfferings(periodId: lastPeriod.Id);

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(1);
        offerings.Items[0].Period.Should().Be(lastPeriod.Name);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_filter_offerings_by_session()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campus = await client.CreateCampus().Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var period = await client.ShortcutGetFirstAcademicPeriod();

        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Morning);
        await client.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id, CourseSession.Evening);

        // Act
        var result = await client.GetCourseOfferings(session: CourseSession.Morning);

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(1);
        offerings.Items[0].Session.Should().Be(CourseSession.Morning);
    }

    [Test]
    public async Task CourseOfferings_GetCourseOfferings_Should_combine_filters()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var campusA = await client.CreateCampus("Agreste I").Success();
        var campusB = await client.CreateCampus("Agreste II").Success();
        var course = await client.CreateCourse().Success();
        var curriculum = await client.CreateCourseCurriculum(course.Id).Success();
        var firstPeriod = await client.ShortcutGetFirstAcademicPeriod();
        var lastPeriod = await client.ShortcutGetLastAcademicPeriod();

        await client.CreateCourseOffering(campusA.Id, course.Id, curriculum.Id, lastPeriod.Id, CourseSession.Morning);
        await client.CreateCourseOffering(campusB.Id, course.Id, curriculum.Id, firstPeriod.Id, CourseSession.Morning);
        await client.CreateCourseOffering(campusB.Id, course.Id, curriculum.Id, lastPeriod.Id, CourseSession.Evening);
        var expected = await client.CreateCourseOffering(campusB.Id, course.Id, curriculum.Id, lastPeriod.Id, CourseSession.Morning).Success();

        // Act
        var result = await client.GetCourseOfferings(campusId: campusB.Id, periodId: lastPeriod.Id, session: CourseSession.Morning);

        // Assert
        var offerings = result.Success;
        offerings.Total.Should().Be(1);
        offerings.Items[0].Id.Should().Be(expected.Id);
    }

    #endregion
}

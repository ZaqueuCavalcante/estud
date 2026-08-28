namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_not_get_discipline_details_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetDisciplineDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_not_get_discipline_details_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetDisciplineDetails(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_not_get_discipline_details_when_discipline_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetDisciplineDetails(999999);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_not_get_other_institution_discipline_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var otherClient = await _back.LoggedAsDirector();
        var otherDiscipline = await otherClient.CreateDiscipline().Success();

        // Act
        var result = await client.GetDisciplineDetails(otherDiscipline.Id);

        // Assert
        result.ShouldBeError(DisciplineNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_get_discipline_details_without_courses_teachers_and_classes()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline("Cálculo I").Success();

        // Act
        var result = await client.GetDisciplineDetails(discipline.Id);

        // Assert
        var details = result.Success;
        details.Id.Should().Be(discipline.Id);
        details.Name.Should().Be("Cálculo I");
        details.Code.Should().NotBeEmpty();
        details.Courses.Should().BeEmpty();
        details.Teachers.Should().BeEmpty();
        details.Classes.Should().BeEmpty();
    }

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_get_discipline_courses_and_teachers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline("Geometria").Success();
        var otherDiscipline = await client.CreateDiscipline("Fisica").Success();

        var engenharia = await client.CreateCourse("Engenharia", CourseType.Bacharelado).Success();
        var ads = await client.CreateCourse("ADS", CourseType.Tecnologo).Success();
        await client.AssignCoursesToDiscipline(discipline.Id, [engenharia.Id, ads.Id]);

        var chico = await client.CreateTeacher("Chico Ferreira", DataGen.Email).Success();
        var ana = await client.CreateTeacher("Ana Lima", DataGen.Email).Success();
        var bruno = await client.CreateTeacher("Bruno Alves", DataGen.Email).Success();
        await client.AssignDisciplinesToTeacher(chico.Id, [discipline.Id]);
        await client.AssignDisciplinesToTeacher(ana.Id, [discipline.Id]);
        await client.AssignDisciplinesToTeacher(bruno.Id, [otherDiscipline.Id]);

        // Act
        var result = await client.GetDisciplineDetails(discipline.Id);

        // Assert
        var details = result.Success;
        details.Courses.Select(x => x.Id).Should().Equal(ads.Id, engenharia.Id);
        details.Courses.Select(x => x.Name).Should().Equal("ADS", "Engenharia");
        details.Teachers.Select(x => x.Id).Should().Equal(ana.Id, chico.Id);
        details.Teachers.Select(x => x.Name).Should().Equal("Ana Lima", "Chico Ferreira");
    }

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_get_only_the_classes_of_the_discipline()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var discipline = await client.CreateDiscipline("Banco de Dados").Success();
        var otherDiscipline = await client.CreateDiscipline("Fisica").Success();

        var period = await client.GetFirstAcademicPeriod();
        var campus = await client.CreateCampus("Agreste I").Success();

        var @class = await client.CreateClass(discipline.Id, period.Id, vacancies: 40, campusId: campus.Id).Success();

        await client.CreateClass(otherDiscipline.Id, period.Id);

        // Act
        var result = await client.GetDisciplineDetails(discipline.Id);

        // Assert
        var classes = result.Success.Classes;
        classes.Should().HaveCount(1);
        classes[0].Id.Should().Be(@class.Id);
        classes[0].Period.Should().Be(period.Name);
        classes[0].Campus.Should().Be("Agreste I");
        classes[0].Vacancies.Should().Be(40);
        classes[0].Students.Should().Be(0);
        classes[0].Status.Should().Be(ClassStatus.OnPreEnrollment);
    }

    [Test]
    public async Task Disciplines_GetDisciplineDetails_Should_get_class_as_awaiting_start_when_there_is_no_open_enrollment_period()
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
        var result = await client.GetDisciplineDetails(discipline.Id);

        // Assert
        result.Success.Classes[0].Status.Should().Be(ClassStatus.OnReview);
    }

    #endregion
}

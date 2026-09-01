namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GetEnrollmentProofs_Should_not_get_proofs_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetEnrollmentProofs();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GetEnrollmentProofs_Should_not_get_proofs_when_user_is_a_director()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetEnrollmentProofs();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GetEnrollmentProofs_Should_not_get_proofs_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetEnrollmentProofs();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GetEnrollmentProofs_Should_get_an_empty_list_when_student_never_generated_a_proof()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();

        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.GetEnrollmentProofs();

        // Assert
        var proofs = result.Success;
        proofs.Total.Should().Be(0);
        proofs.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Students_GetEnrollmentProofs_Should_get_the_proofs_of_the_logged_student_ordered_by_issue_date()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var campus = await director.CreateCampus().Success();
        var course = await director.CreateCourse().Success();
        var curriculum = await director.CreateCourseCurriculum(course.Id).Success();
        var period = await director.GetFirstAcademicPeriod();
        var offering = await director.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.EnrollStudentInCourseOffering(student.Id, offering.Id);

        var other = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        await director.EnrollStudentInCourseOffering(other.Id, offering.Id);

        var client = await _back.LoginAs(student.Email);
        await client.GenerateEnrollmentProof();
        await client.GenerateEnrollmentProof();

        var otherClient = await _back.LoginAs(other.Email);
        await otherClient.GenerateEnrollmentProof();

        // Act
        var result = await client.GetEnrollmentProofs();

        // Assert
        var proofs = result.Success;
        proofs.Total.Should().Be(2);
        proofs.Items.Should().HaveCount(2);
        proofs.Items.Should().OnlyContain(p => p.Code.StartsWith("ESTUD-"));
        proofs.Items.Select(p => p.Code).Should().OnlyHaveUniqueItems();
        proofs.Items.Should().BeInDescendingOrder(p => p.IssuedAt);
        proofs.Items[0].IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    #endregion
}

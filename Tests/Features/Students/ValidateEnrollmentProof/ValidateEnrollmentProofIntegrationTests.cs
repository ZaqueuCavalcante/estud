namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    public async Task Students_ValidateEnrollmentProof_Should_not_validate_when_code_does_not_exist()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.ValidateEnrollmentProof("ESTUD-2026-LALALA");

        // Assert
        result.ShouldBeError(EnrollmentProofNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_ValidateEnrollmentProof_Should_validate_a_generated_proof_without_authentication()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var campus = await director.CreateCampus().Success();
        var course = await director.CreateCourse().Success();
        var curriculum = await director.CreateCourseCurriculum(course.Id).Success();
        var period = await director.GetFirstAcademicPeriod();
        var offering = await director.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var studentName = DataGen.UserName;
        var student = await director.CreateStudent(studentName, DataGen.Email).Success();
        await director.EnrollStudentInCourseOffering(student.Id, offering.Id);

        var client = await _back.LoginAs(student.Email);
        await client.GenerateEnrollmentProof();

        var proofs = await client.GetEnrollmentProofs().Success();
        var code = proofs.Items[0].Code;

        var anonymous = _back.GetTestsClient();

        // Act
        var result = await anonymous.ValidateEnrollmentProof(code);

        // Assert
        var proof = result.Success;
        proof.Code.Should().Be(code);
        proof.StudentName.Should().Be(studentName);
        proof.Course.Should().Be("Análise e Desenvolvimento de Sistemas");
        proof.Session.Should().Be(CourseSession.Evening);
        proof.Institution.Should().NotBeNullOrEmpty();
        proof.Campus.Should().NotBeNullOrEmpty();
        proof.Period.Should().NotBeNullOrEmpty();
        proof.EnrollmentCode.Should().NotBeNullOrEmpty();
        proof.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    #endregion
}

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
        var (email, studentName) = await ArrangeEnrolledStudent();
        var student = await _back.LoginAs(email);
        await student.GenerateEnrollmentProof();

        var code = await GetLastEnrollmentProofCode();

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

    private async Task<string> GetLastEnrollmentProofCode()
    {
        await using var ctx = _back.GetDbContext();
        return await ctx.EnrollmentProofs.AsNoTracking()
            .OrderByDescending(p => p.Id)
            .Select(p => p.Code)
            .FirstAsync();
    }
}

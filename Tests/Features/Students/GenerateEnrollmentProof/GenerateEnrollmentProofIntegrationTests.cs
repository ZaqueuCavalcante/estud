using System.Text;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Students_GenerateEnrollmentProof_Should_not_generate_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GenerateEnrollmentProof();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Students_GenerateEnrollmentProof_Should_not_generate_when_user_is_a_director()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GenerateEnrollmentProof();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Students_GenerateEnrollmentProof_Should_not_generate_when_user_is_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GenerateEnrollmentProof();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Students_GenerateEnrollmentProof_Should_not_generate_when_student_is_not_enrolled_in_any_course()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var email = DataGen.Email;
        await director.CreateStudent(DataGen.UserName, email);

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.GenerateEnrollmentProof();

        // Assert
        result.ShouldBeError(StudentNotEnrolledInAnyCourse.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Students_GenerateEnrollmentProof_Should_generate_a_pdf_for_an_enrolled_student()
    {
        // Arrange
        var (email, _) = await ArrangeEnrolledStudent();
        var client = await _back.LoginAs(email);

        // Act
        var result = await client.GenerateEnrollmentProof();

        // Assert
        var pdf = result.Success;
        pdf.Should().NotBeEmpty();
        Encoding.ASCII.GetString(pdf, 0, 5).Should().Be("%PDF-");
    }

    #endregion

    /// <summary>
    /// Cria um aluno já matriculado numa oferta de curso e retorna o e-mail (para login) e o nome do aluno.
    /// </summary>
    private async Task<(string email, string studentName)> ArrangeEnrolledStudent()
    {
        var director = await _back.LoggedAsDirector();

        var campus = await director.CreateCampus().Success();
        var course = await director.CreateCourse().Success();
        var curriculum = await director.CreateCourseCurriculum(course.Id).Success();
        var period = await director.GetFirstAcademicPeriod();
        var offering = await director.CreateCourseOffering(campus.Id, course.Id, curriculum.Id, period.Id).Success();

        var name = DataGen.UserName;
        var email = DataGen.Email;
        var student = await director.CreateStudent(name, email).Success();
        await director.EnrollStudentInCourseOffering(student.Id, offering.Id);

        return (email, name);
    }
}

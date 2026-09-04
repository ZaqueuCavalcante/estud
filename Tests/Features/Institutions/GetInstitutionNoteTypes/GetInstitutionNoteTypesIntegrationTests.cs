namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Institutions_GetInstitutionNoteTypes_Should_not_get_note_types_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Institutions_GetInstitutionNoteTypes_Should_not_get_note_types_when_user_is_not_a_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Institutions_GetInstitutionNoteTypes_Should_get_the_three_note_types_by_default()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.Success.NoteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3);
    }

    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public async Task Institutions_GetInstitutionNoteTypes_Should_get_the_three_note_types(ClassGradeRule rule)
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: rule);

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.Success.NoteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3);
    }

    [Test]
    public async Task Institutions_GetInstitutionNoteTypes_Should_not_get_the_third_note_type_when_rule_is_average_of_two()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(teacher.Email);

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.Success.NoteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2);
    }

    [Test]
    public async Task Institutions_GetInstitutionNoteTypes_Should_get_the_note_types_of_the_institution_of_the_logged_teacher()
    {
        // Arrange
        var otherDirector = await _back.LoggedAsDirector();
        await otherDirector.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetInstitutionNoteTypes();

        // Assert
        result.Success.NoteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2, ClassNoteType.N3);
    }

    #endregion
}

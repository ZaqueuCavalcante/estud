namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_not_setup_institution_config_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.SetupInstitutionConfig();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_not_setup_institution_config_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.SetupInstitutionConfig();

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(-0.01)]
    [TestCase(-1.00)]
    [TestCase(10.01)]
    [TestCase(11.00)]
    public async Task Institutions_SetupInstitutionConfig_Should_not_setup_institution_config_with_invalid_note_limit(decimal noteLimit)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.SetupInstitutionConfig(noteLimit, 70.00M);

        // Assert
        result.ShouldBeError(InvalidNoteLimit.I);
    }

    [Test]
    [TestCase(-0.01)]
    [TestCase(-1.00)]
    [TestCase(100.01)]
    [TestCase(150.00)]
    public async Task Institutions_SetupInstitutionConfig_Should_not_setup_institution_config_with_invalid_frequency_limit(decimal frequencyLimit)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.SetupInstitutionConfig(7.00M, frequencyLimit);

        // Assert
        result.ShouldBeError(InvalidFrequencyLimit.I);
    }

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_not_setup_institution_config_with_unknown_grade_rule()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.SetupInstitutionConfig(gradeRule: (ClassGradeRule)69);

        // Assert
        result.ShouldBeError(InvalidClassGradeRule.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_create_institution_config_with_default_values_on_institution_creation()
    {
        // Arrange / Act
        var client = await _back.LoggedAsDirector();

        // Assert
        var result = await client.GetInstitutionConfig();
        var config = result.Success;
        config.NoteLimit.Should().Be(7.00M);
        config.FrequencyLimit.Should().Be(70.00M);
        config.GradeRule.Should().Be(ClassGradeRule.BestTwoOfThree);
    }

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_setup_institution_config()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.SetupInstitutionConfig(8.50M, 85.00M, ClassGradeRule.AverageOfThree);

        // Assert
        var config = result.Success;
        config.NoteLimit.Should().Be(8.50M);
        config.FrequencyLimit.Should().Be(85.00M);
        config.GradeRule.Should().Be(ClassGradeRule.AverageOfThree);

        var saved = await client.GetInstitutionConfig().Success();
        saved.Id.Should().Be(config.Id);
        saved.NoteLimit.Should().Be(8.50M);
        saved.FrequencyLimit.Should().Be(85.00M);
        saved.GradeRule.Should().Be(ClassGradeRule.AverageOfThree);
    }

    [TestCase(ClassGradeRule.BestTwoOfThree)]
    [TestCase(ClassGradeRule.AverageOfTwo)]
    [TestCase(ClassGradeRule.AverageOfThree)]
    [TestCase(ClassGradeRule.AverageOrThird)]
    public async Task Institutions_SetupInstitutionConfig_Should_setup_every_grade_rule(ClassGradeRule gradeRule)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.SetupInstitutionConfig(gradeRule: gradeRule);

        // Assert
        result.Success.GradeRule.Should().Be(gradeRule);
    }

    [Test]
    public async Task Institutions_SetupInstitutionConfig_Should_change_the_note_types_used_by_the_institution()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var teacherClient = await _back.LoginAs(teacher.Email);

        // Act
        await director.SetupInstitutionConfig(gradeRule: ClassGradeRule.AverageOfTwo);

        // Assert
        var noteTypes = await teacherClient.GetInstitutionNoteTypes().Success();
        noteTypes.NoteTypes.Should().Equal(ClassNoteType.N1, ClassNoteType.N2);
    }

    #endregion
}

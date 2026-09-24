using System.Text.Json;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Validation errors

    [Test]
    [TestCase("Admin")]
    [TestCase("99")]
    [TestCase("")]
    [TestCase(99)]
    [TestCase(-1)]
    [TestCase(1.5)]
    public async Task Converters_EstudStringEnumConverter_Should_reject_invalid_enum_as_domain_error(object baseType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRoleWithRawBaseType(baseType);

        // Assert
        result.ShouldBeError(InvalidRoleBaseType.I);
    }

    [Test]
    [TestCase("XX")]
    [TestCase("99")]
    [TestCase("")]
    [TestCase(99)]
    [TestCase(null)]
    public async Task Converters_EstudStringEnumConverter_Should_read_invalid_nullable_enum_as_null(object? state)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCampusWithRawState(state);

        // Assert
        result.ShouldBeError(InvalidBrazilState.I);
    }

    #endregion

    #region Happy path

    [Test]
    [TestCase("Teacher")]
    [TestCase("teacher")]
    [TestCase("TEACHER")]
    [TestCase("1")]
    [TestCase(1)]
    public async Task Converters_EstudStringEnumConverter_Should_read_enum_as_string_or_int(object baseType)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateRoleWithRawBaseType(baseType);

        // Assert
        var role = await client.GetRole(result.Success.Id);
        role.Success.BaseType.Should().Be(UserType.Teacher);
    }

    [Test]
    [TestCase("PE")]
    [TestCase("pe")]
    [TestCase("16")]
    [TestCase(16)]
    public async Task Converters_EstudStringEnumConverter_Should_read_nullable_enum_as_string_or_int(object state)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.CreateCampusWithRawState(state);

        // Assert
        var campusId = result.Success.Id;
        var campi = await client.GetCampi();
        campi.Success.Items.Should().ContainSingle(x => x.Id == campusId).Which.State.Should().Be(BrazilState.PE);
    }

    [Test]
    public async Task Converters_EstudStringEnumConverter_Should_write_enum_as_string()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var role = await client.CreateRoleWithRawBaseType(1);

        // Act
        var response = await client.GetRoleRaw(role.Success.Id);

        // Assert
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var baseType = json.RootElement.GetProperty("baseType");
        baseType.ValueKind.Should().Be(JsonValueKind.String);
        baseType.GetString().Should().Be("Teacher");
    }

    #endregion
}

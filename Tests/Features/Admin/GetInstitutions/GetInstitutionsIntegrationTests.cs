namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Admin_GetInstitutions_Should_not_list_when_not_authenticated()
    {
        // Arrange
        var admin = _back.GetTestsClient();

        // Act
        var result = await admin.GetInstitutions();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Admin_GetInstitutions_Should_list_institutions_across_tenants()
    {
        // Arrange
        var directorA = await _back.LoggedAsDirector();
        var directorB = await _back.LoggedAsDirector();

        var admin = await _back.LoggedAsAdm();

        // Act
        var institutions = await admin.GetInstitutions(pageSize: 100).Success();

        // Assert
        institutions.Total.Should().BeGreaterThanOrEqualTo(2);
        institutions.Items.Select(i => i.Id).Should().OnlyHaveUniqueItems();
    }

    #endregion
}

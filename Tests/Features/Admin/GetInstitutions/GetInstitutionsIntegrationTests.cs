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
        // Arrange — cada registro cria uma instituição nova (dois tenants diferentes).
        var directorA = await _back.LoggedAsDirector();
        var directorB = await _back.LoggedAsDirector();
        var instA = directorA.User.InstitutionId;
        var instB = directorB.User.InstitutionId;

        // Act
        var admin = await _back.LoggedAsAdm();
        var result = await admin.GetInstitutions(pageSize: 100);

        // Assert — o endpoint atravessa instituições: as duas aparecem na mesma listagem.
        var page = result.Success;
        var ids = page.Items.Select(i => i.Id).ToList();
        ids.Should().Contain(instA);
        ids.Should().Contain(instB);

        page.Items.Single(i => i.Id == instA).UsersCount.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Admin_GetDomainEvents_Should_not_list_when_not_authenticated()
    {
        // Arrange
        var admin = _back.GetTestsClient();

        // Act
        var result = await admin.GetDomainEvents();

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Admin_GetDomainEvents_Should_list_domain_events_across_tenants()
    {
        // Arrange
        var directorA = await _back.LoggedAsDirector();
        var directorB = await _back.LoggedAsDirector();
    
        await directorA.CreateStudent(DataGen.UserName, DataGen.Email);
        await directorB.CreateStudent(DataGen.UserName, DataGen.Email);

        var admin = await _back.LoggedAsAdm();

        // Act
        var events = await admin.GetDomainEvents().Success();

        // Assert
        events.Total.Should().Be(2);
        events.Items.Select(e => e.InstitutionId).Should().OnlyHaveUniqueItems();
    }

    #endregion
}

using Estud.Back.Domain.DomainEvents;

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
    public async Task Admin_GetDomainEvents_Should_list_events_across_tenants_and_filter()
    {
        // Arrange — dois tenants, um evento em cada. Não há endpoint que produza um domain event
        // isolado (são efeito colateral de fluxos pesados), então semeamos direto no banco
        // compartilhado — caso permitido pela convenção quando não há endpoint para o estado.
        var directorA = await _back.LoggedAsDirector();
        var directorB = await _back.LoggedAsDirector();
        var instA = directorA.User.InstitutionId;
        var instB = directorB.User.InstitutionId;

        var uidA = Guid.NewGuid().ToString("N")[..26];
        var uidB = Guid.NewGuid().ToString("N")[..26];

        await using (var ctx = _back.GetDbContext())
        {
            ctx.DomainEvents.Add(new DomainEvent
            {
                InstitutionId = instA, EntityUid = uidA, Type = "Estud.Tests.FakeEventA",
                Data = "{}", Status = DomainEventStatus.Success, OccurredAt = DateTime.UtcNow,
            });
            ctx.DomainEvents.Add(new DomainEvent
            {
                InstitutionId = instB, EntityUid = uidB, Type = "Estud.Tests.FakeEventB",
                Data = "{}", Status = DomainEventStatus.Error, Error = "boom", OccurredAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        var admin = await _back.LoggedAsAdm();

        // Act + Assert — cross-tenant: o mesmo endpoint enxerga os eventos das duas instituições.
        var eventA = (await admin.GetDomainEvents(entityUid: uidA)).Success;
        eventA.Total.Should().Be(1);
        eventA.Items.Single().InstitutionId.Should().Be(instA);
        eventA.Items.Single().Status.Should().Be(DomainEventStatus.Success);

        var eventB = (await admin.GetDomainEvents(entityUid: uidB)).Success;
        eventB.Total.Should().Be(1);
        eventB.Items.Single().InstitutionId.Should().Be(instB);

        // Filtro por status=Error (o caso de uso principal).
        var errorHit = (await admin.GetDomainEvents(status: DomainEventStatus.Error, entityUid: uidB)).Success;
        errorHit.Total.Should().Be(1);
        errorHit.Items.Single().Error.Should().Be("boom");

        var errorMiss = (await admin.GetDomainEvents(status: DomainEventStatus.Success, entityUid: uidB)).Success;
        errorMiss.Total.Should().Be(0);

        // Filtro por instituição.
        var byInstMiss = (await admin.GetDomainEvents(institutionId: instA, entityUid: uidB)).Success;
        byInstMiss.Total.Should().Be(0);
    }

    #endregion
}

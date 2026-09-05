using System.Text.Json;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Happy path

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_to_target_with_custom_headers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        await using var ctx = _back.GetDbContext();
        var call = await ctx.WebhookCalls.Include(x => x.Attempts)
            .AsNoTracking().FirstAsync(x => x.WebhookSubscriptionId == subscription.Id);

        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(1);

        var attempt = call.Attempts.Single();
        attempt.Status.Should().Be(WebhookCallAttemptStatus.Success);
        attempt.StatusCode.Should().Be(200);
        attempt.Response.Should().Contain("secret-key-123");
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_register_error_when_target_responds_with_error()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target/error",
            events: [WebhookEventType.StudentCreated],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        await using var ctx = _back.GetDbContext();
        var call = await ctx.WebhookCalls.Include(x => x.Attempts)
            .AsNoTracking().FirstAsync(x => x.WebhookSubscriptionId == subscription.Id);

        call.Status.Should().Be(WebhookCallStatus.Error);

        var attempt = call.Attempts.Single();
        attempt.Status.Should().Be(WebhookCallAttemptStatus.Error);
        attempt.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_only_once()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act - a second processing round must not re-enqueue an already handled call
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        await using var ctx = _back.GetDbContext();
        var call = await ctx.WebhookCalls.Include(x => x.Attempts)
            .AsNoTracking().FirstAsync(x => x.WebhookSubscriptionId == subscription.Id);

        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(1);
        call.Attempts.Should().HaveCount(1);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_send_unique_event_id_in_payload()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        await client.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await client.GetWebhookCalls().Success();
        var call = await client.GetWebhookCall(calls.Items.Single().Id).Success();

        call.Uid.Should().NotBeNullOrEmpty();

        using var payload = JsonDocument.Parse(call.Payload);
        payload.RootElement.GetProperty("Id").GetString().Should().Be(call.Uid);
        payload.RootElement.GetProperty("EventType").GetString().Should().Be(nameof(WebhookEventType.StudentCreated));
        payload.RootElement.TryGetProperty("OccurredAt", out _).Should().BeTrue();

        call.Attempts.Single().Response.Should().Contain(call.Uid);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_send_a_different_event_id_to_each_subscription()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        await client.CreateWebhookSubscription(
            name: "Assinatura 1",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateWebhookSubscription(
            name: "Assinatura 2",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await client.GetWebhookCalls().Success();

        calls.Items.Should().HaveCount(2);
        calls.Items.Select(x => x.Uid).Should().OnlyHaveUniqueItems();
        calls.Items.Should().OnlyContain(x => x.Uid != null && x.Uid.Length > 0);
    }

    #endregion
}

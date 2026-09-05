namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_not_get_webhook_call_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetWebhookCall(callId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_not_get_webhook_call_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetWebhookCall(callId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_not_get_webhook_call_when_it_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetWebhookCall(callId: 999999);

        // Assert
        result.ShouldBeError(WebhookCallNotFound.I);
    }

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_not_get_webhook_call_of_another_institution()
    {
        // Arrange
        var client1 = await _back.LoggedAsDirector();
        await client1.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]);
        await client1.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        var call = await client1.GetWebhookCalls().Success();
        var callId = call.Items.Single().Id;

        var client2 = await _back.LoggedAsDirector();

        // Act
        var result = await client2.GetWebhookCall(callId);

        // Assert
        result.ShouldBeError(WebhookCallNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_get_webhook_call_details()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            name: "Aluno criado",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        var calls = await client.GetWebhookCalls().Success();
        var callId = calls.Items.Single().Id;

        // Act
        var result = await client.GetWebhookCall(callId);

        // Assert
        var call = result.Success;
        call.Id.Should().Be(callId);
        call.EventType.Should().Be(WebhookEventType.StudentCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(1);
        call.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        call.Payload.Should().Contain("StudentCreated");

        call.Subscription.Id.Should().Be(subscription.Id);
        call.Subscription.Name.Should().Be("Aluno criado");
        call.Subscription.Url.Should().Be($"{MocksFactory.Url}/webhooks/target");
        call.Subscription.IsActive.Should().BeTrue();

        call.Request.Method.Should().Be("POST");
        call.Request.Url.Should().Be($"{MocksFactory.Url}/webhooks/target");
        call.Request.Headers.Should().Contain(new KeyValuePair<string, string>("X-Api-Key", "secret-key-123"));
        call.Request.Body.Should().Be(call.Payload);

        var attempt = call.Attempts.Single();
        attempt.Id.Should().BePositive();
        attempt.Status.Should().Be(WebhookCallAttemptStatus.Success);
        attempt.StatusCode.Should().Be(200);
        attempt.Response.Should().Contain("secret-key-123");
        attempt.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        attempt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task Webhooks_GetWebhookCall_Should_get_webhook_call_details_when_call_failed()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        await client.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target/error",
            events: [WebhookEventType.StudentCreated]);

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        var calls = await client.GetWebhookCalls().Success();
        var callId = calls.Items.Single().Id;

        // Act
        var result = await client.GetWebhookCall(callId);

        // Assert
        var call = result.Success;
        call.Status.Should().Be(WebhookCallStatus.Error);

        var attempt = call.Attempts.Single();
        attempt.Status.Should().Be(WebhookCallAttemptStatus.Error);
        attempt.StatusCode.Should().Be(500);
    }

    #endregion
}

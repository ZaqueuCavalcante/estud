using Estud.Tests.Integration.Clients;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_not_get_webhook_calls_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.GetWebhookCalls(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_not_get_webhook_calls_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.GetWebhookCalls(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_not_get_webhook_calls_when_subscription_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.GetWebhookCalls(999999);

        // Assert
        result.ShouldBeError(WebhookSubscriptionNotFound.I);
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_not_get_webhook_calls_when_subscription_is_from_another_institution()
    {
        // Arrange
        var client1 = await _back.LoggedAsDirector();
        var subscription = await client1.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();
        await client1.CreateStudent(DataGen.UserName, DataGen.Email);

        var client2 = await _back.LoggedAsDirector();

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client2.GetWebhookCalls(subscription.Id);

        // Assert
        result.ShouldBeError(WebhookSubscriptionNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_empty_list_when_no_webhook_calls_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var subscription = await client.CreateWebhookSubscription().Success();

        // Act
        var result = await client.GetWebhookCalls(subscription.Id);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(0);
        calls.Page.Should().Be(1);
        calls.PageSize.Should().Be(20);
        calls.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_webhook_calls()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client.GetWebhookCalls(subscription.Id);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(1);
        calls.Items.Should().HaveCount(1);

        var call = calls.Items.Single();
        call.Id.Should().BePositive();
        call.Uid.Should().NotBeNullOrEmpty();
        call.EventType.Should().Be(WebhookEventType.StudentCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(1);
        call.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_webhook_calls_paginated_ordered_by_created_at_desc()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);
        await client.CreateStudent(DataGen.UserName, DataGen.Email);
        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var firstPage = await client.GetWebhookCalls(subscription.Id, page: 1, pageSize: 2).Success();
        var secondPage = await client.GetWebhookCalls(subscription.Id, page: 2, pageSize: 2).Success();

        // Assert
        firstPage.Total.Should().Be(3);
        firstPage.Items.Should().HaveCount(2);

        secondPage.Total.Should().Be(3);
        secondPage.Items.Should().HaveCount(1);

        var allIds = firstPage.Items.Concat(secondPage.Items).Select(x => x.Id).ToList();
        allIds.Should().OnlyHaveUniqueItems();

        firstPage.Items.Should().BeInDescendingOrder(x => x.CreatedAt);
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_only_own_institution_webhook_calls()
    {
        // Arrange
        var client1 = await _back.LoggedAsDirector();
        await client1.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]);
        await client1.CreateStudent(DataGen.UserName, DataGen.Email);

        var client2 = await _back.LoggedAsDirector();
        var subscription2 = await client2.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();
        await client2.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client2.GetWebhookCalls(subscription2.Id);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(1);
        calls.Items.Should().HaveCount(1);
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_only_webhook_calls_of_the_given_subscription()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            name: "Destino ok",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();
        await client.CreateWebhookSubscription(
            name: "Destino com erro",
            url: $"{FakesFactory.Url}/webhooks/target/error",
            events: [WebhookEventType.StudentCreated]);

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client.GetWebhookCalls(subscription.Id);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(1);
        calls.Items.Should().ContainSingle().Which.Status.Should().Be(WebhookCallStatus.Success);
    }

    [Test]
    [TestCase(WebhookCallStatus.Success)]
    [TestCase(WebhookCallStatus.Error)]
    public async Task Webhooks_GetWebhookCalls_Should_get_only_webhook_calls_with_the_given_status(WebhookCallStatus status)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscriptionId = await CreateWebhookSubscriptionWithSuccessAndErrorCalls(client);

        // Act
        var result = await client.GetWebhookCalls(subscriptionId, status: status);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(1);
        calls.Items.Should().ContainSingle().Which.Status.Should().Be(status);
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_webhook_calls_of_all_status_when_status_is_not_informed()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscriptionId = await CreateWebhookSubscriptionWithSuccessAndErrorCalls(client);

        // Act
        var result = await client.GetWebhookCalls(subscriptionId);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(2);
        calls.Items.Select(x => x.Status).Should().BeEquivalentTo([WebhookCallStatus.Success, WebhookCallStatus.Error]);
    }

    [Test]
    public async Task Webhooks_GetWebhookCalls_Should_get_empty_list_when_no_webhook_call_has_the_given_status()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client.GetWebhookCalls(subscription.Id, status: WebhookCallStatus.Error);

        // Assert
        var calls = result.Success;
        calls.Total.Should().Be(0);
        calls.Items.Should().BeEmpty();
    }

    #endregion

    private async Task<int> CreateWebhookSubscriptionWithSuccessAndErrorCalls(TestsHttpClient client)
    {
        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        await client.UpdateWebhookSubscription(subscription.Id, url: $"{FakesFactory.Url}/webhooks/target/error").Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        return subscription.Id;
    }
}

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_when_user_has_no_permission()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    [TestCase(-1)]
    [TestCase(6)]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_with_invalid_max_retries(int maxRetries)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(1, maxRetries: maxRetries);

        // Assert
        result.ShouldBeError(InvalidWebhookMaxRetries.I);
    }

    [Test]
    [TestCase(-1)]
    [TestCase(31)]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_with_invalid_base_delay_seconds(int baseDelaySeconds)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(1, baseDelaySeconds: baseDelaySeconds);

        // Assert
        result.ShouldBeError(InvalidWebhookBaseDelaySeconds.I);
    }

    [Test]
    [TestCase(null)]
    [TestCase(99)]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_with_invalid_backoff_strategy(int? backoffStrategy)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(1, backoffStrategy: (BackoffStrategy?)backoffStrategy);

        // Assert
        result.ShouldBeError(InvalidWebhookBackoffStrategy.I);
    }

    [Test]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_update_retry_configs_when_subscription_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(999999);

        // Assert
        result.ShouldBeError(WebhookSubscriptionNotFound.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_update_retry_configs()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var created = await client.CreateWebhookSubscription().Success();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(
            created.Id,
            maxRetries: 4,
            baseDelaySeconds: 15,
            backoffStrategy: BackoffStrategy.Fixed);

        // Assert
        result.ShouldBeSuccess();

        var subscription = await client.GetWebhookSubscription(created.Id).Success();
        subscription.MaxRetries.Should().Be(4);
        subscription.BaseDelaySeconds.Should().Be(15);
        subscription.BackoffStrategy.Should().Be(BackoffStrategy.Fixed);
    }

    [Test]
    [TestCase(0, 0, BackoffStrategy.None)]
    [TestCase(5, 30, BackoffStrategy.Linear)]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_accept_boundary_values(
        int maxRetries, int baseDelaySeconds, BackoffStrategy backoffStrategy)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var created = await client.CreateWebhookSubscription().Success();

        // Act
        var result = await client.UpdateWebhookSubscriptionRetryConfigs(created.Id, maxRetries, baseDelaySeconds, backoffStrategy);

        // Assert
        result.ShouldBeSuccess();

        var subscription = await client.GetWebhookSubscription(created.Id).Success();
        subscription.MaxRetries.Should().Be(maxRetries);
        subscription.BaseDelaySeconds.Should().Be(baseDelaySeconds);
        subscription.BackoffStrategy.Should().Be(backoffStrategy);
    }

    [Test]
    public async Task Webhooks_UpdateWebhookSubscriptionRetryConfigs_Should_not_change_other_subscription_data()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var created = await client.CreateWebhookSubscription(
            name: "Professor criado",
            url: "https://webhook.site/teachers",
            events: [WebhookEventType.TeacherCreated],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        // Act
        await client.UpdateWebhookSubscriptionRetryConfigs(created.Id, maxRetries: 2).Success();

        // Assert
        var subscription = await client.GetWebhookSubscription(created.Id).Success();
        subscription.Name.Should().Be("Professor criado");
        subscription.Url.Should().Be("https://webhook.site/teachers");
        subscription.IsActive.Should().BeTrue();
        subscription.Events.Should().BeEquivalentTo([WebhookEventType.TeacherCreated]);
        subscription.CustomHeaders.Should().ContainKey("X-Api-Key").WhoseValue.Should().Be("secret-key-123");
    }

    #endregion
}

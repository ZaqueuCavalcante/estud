using Estud.Back.Domain.Identity;
using Estud.Tests.Integration.Clients;

namespace Estud.Tests.Integration;

public partial class IntegrationTests
{
    #region Authentication

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_when_not_authenticated()
    {
        // Arrange
        var client = _back.GetTestsClient();

        // Act
        var result = await client.RetryWebhookCall(callId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_when_user_is_teacher()
    {
        // Arrange
        var client = await _back.LoggedAsTeacher();

        // Act
        var result = await client.RetryWebhookCall(callId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_when_user_is_student()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();
        var student = await director.CreateStudent(DataGen.UserName, DataGen.Email).Success();
        var client = await _back.LoginAs(student.Email);

        // Act
        var result = await client.RetryWebhookCall(callId: 1);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_when_manager_has_no_permission()
    {
        // Arrange
        var email = DataGen.Email;
        var director = await _back.LoggedAsDirector(email);
        var (_, callId) = await CreateFailedWebhookCall(director);

        var limitedRole = await director.CreateRole(name: "Gerente de Perfis", permissions: [EstudPermissions.ManageRoles.Id]).Success();
        var limitedRoleId = limitedRole.Id;
        var userId = director.User.Id;

        await using (var ctx = _back.GetDbContext())
        {
            var userRole = await ctx.UserRoles.FirstAsync(x => x.UserId == userId);
            ctx.Remove(userRole);
            ctx.Add(new EstudUserRole(userRole.InstitutionId, userRole.UserId, limitedRoleId));
            await ctx.SaveChangesAsync();
        }

        var client = await _back.LoginAs(email);

        // Act
        var result = await client.RetryWebhookCall(callId);

        // Assert
        result.ShouldBeError(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Validation errors

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_when_it_does_not_exist()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        // Act
        var result = await client.RetryWebhookCall(callId: 999999);

        // Assert
        result.ShouldBeError(WebhookCallNotFound.I);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_of_another_institution()
    {
        // Arrange
        var client1 = await _back.LoggedAsDirector();
        var (_, callId) = await CreateFailedWebhookCall(client1);

        var client2 = await _back.LoggedAsDirector();

        // Act
        var result = await client2.RetryWebhookCall(callId);

        // Assert
        result.ShouldBeError(WebhookCallNotFound.I);

        await _back.AwaitCommandsProcessing();
        var call = await client1.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Error);
        call.AttemptsCount.Should().Be(1);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_that_succeeded()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]);
        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        var calls = await client.GetWebhookCalls().Success();
        var callId = calls.Items.Single().Id;

        // Act
        var result = await client.RetryWebhookCall(callId);

        // Assert
        result.ShouldBeError(WebhookCallCannotBeRetried.I);

        await _back.AwaitCommandsProcessing();
        var call = await client.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(1);
    }

    [Test]
    [TestCase(WebhookCallStatus.Pending)]
    [TestCase(WebhookCallStatus.Processing)]
    public async Task Webhooks_RetryWebhookCall_Should_not_retry_webhook_call_that_is_not_finished(WebhookCallStatus status)
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var (_, callId) = await CreateFailedWebhookCall(client);

        await using (var ctx = _back.GetDbContext())
        {
            await ctx.WebhookCalls.Where(x => x.Id == callId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status));
        }

        // Act
        var result = await client.RetryWebhookCall(callId);

        // Assert
        result.ShouldBeError(WebhookCallCannotBeRetried.I);
    }

    #endregion

    #region Happy path

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_deliver_webhook_call_when_target_recovers()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var (subscriptionId, callId) = await CreateFailedWebhookCall(client);

        await client.UpdateWebhookSubscription(subscriptionId, url: $"{FakesFactory.Url}/webhooks/target").Success();

        // Act
        var result = await client.RetryWebhookCall(callId);
        await _back.AwaitCommandsProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();

        var call = await client.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.AttemptsCount.Should().Be(2);
        call.Attempts.Should().HaveCount(2);

        var lastAttempt = call.Attempts[0];
        lastAttempt.Status.Should().Be(WebhookCallAttemptStatus.Success);
        lastAttempt.StatusCode.Should().Be(200);
        lastAttempt.Response.Should().Contain(call.Uid);

        var firstAttempt = call.Attempts[1];
        firstAttempt.Status.Should().Be(WebhookCallAttemptStatus.Error);
        firstAttempt.StatusCode.Should().Be(500);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_register_new_failed_attempt_when_target_still_fails()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var (_, callId) = await CreateFailedWebhookCall(client);

        // Act
        var result = await client.RetryWebhookCall(callId);
        await _back.AwaitCommandsProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();

        var call = await client.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Error);
        call.AttemptsCount.Should().Be(2);
        call.Attempts.Should().HaveCount(2);
        call.Attempts.Should().OnlyContain(x => x.Status == WebhookCallAttemptStatus.Error && x.StatusCode == 500);
    }

    [Test]
    public async Task Webhooks_RetryWebhookCall_Should_retry_webhook_call_again_after_a_failed_retry()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();
        var (_, callId) = await CreateFailedWebhookCall(client);

        await client.RetryWebhookCall(callId).Success();
        await _back.AwaitCommandsProcessing();

        // Act
        var result = await client.RetryWebhookCall(callId);
        await _back.AwaitCommandsProcessing();

        // Assert
        result.IsSuccess.Should().BeTrue();

        var call = await client.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Error);
        call.AttemptsCount.Should().Be(3);
        call.Attempts.Should().HaveCount(3);
    }

    #endregion

    private async Task<(int SubscriptionId, int CallId)> CreateFailedWebhookCall(TestsHttpClient client)
    {
        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target/error",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        var calls = await client.GetWebhookCalls().Success();
        return (subscription.Id, calls.Items.Single().Id);
    }
}

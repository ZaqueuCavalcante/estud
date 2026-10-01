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
            url: $"{FakesFactory.Url}/webhooks/target",
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
            url: $"{FakesFactory.Url}/webhooks/target/error",
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
            url: $"{FakesFactory.Url}/webhooks/target",
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

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await client.GetWebhookCalls(subscription.Id).Success();
        var call = await client.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventUid.Should().NotBeNullOrEmpty();

        using var payload = JsonDocument.Parse(call.Payload);
        payload.RootElement.GetProperty("event_id").GetString().Should().Be(call.EventUid);
        payload.RootElement.GetProperty("event_type").GetString().Should().Be(nameof(WebhookEventType.StudentCreated));
        payload.RootElement.TryGetProperty("occurred_at", out _).Should().BeTrue();

        call.Attempts.Single().Response.Should().Contain(call.EventUid);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_send_a_different_event_id_to_each_subscription()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription1 = await client.CreateWebhookSubscription(
            name: "Assinatura 1",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        var subscription2 = await client.CreateWebhookSubscription(
            name: "Assinatura 2",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls1 = await client.GetWebhookCalls(subscription1.Id).Success();
        var calls2 = await client.GetWebhookCalls(subscription2.Id).Success();
        var calls = calls1.Items.Concat(calls2.Items).ToList();

        calls.Should().HaveCount(2);
        calls.Select(x => x.Uid).Should().OnlyHaveUniqueItems();
        calls.Should().OnlyContain(x => x.Uid != null && x.Uid.Length > 0);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_when_teacher_is_created()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var subscription = await director.CreateWebhookSubscription(
            name: "Professor criado",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.TeacherCreated]).Success();

        var email = DataGen.Email;
        await director.CreateTeacher("Maria Oliveira", email).Success();

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await director.GetWebhookCalls(subscription.Id).Success();
        var call = await director.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventType.Should().Be(WebhookEventType.TeacherCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.Subscription.Id.Should().Be(subscription.Id);

        using var payload = JsonDocument.Parse(call.Payload);
        payload.RootElement.GetProperty("event_type").GetString().Should().Be(nameof(WebhookEventType.TeacherCreated));

        var data = payload.RootElement.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("Maria Oliveira");
        data.GetProperty("email").GetString().Should().Be(email);
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_when_class_activity_is_created()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var subscription = await director.CreateWebhookSubscription(
            name: "Atividade publicada",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.ClassActivityPublished],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var teacherClient = await _back.LoginAs(teacher.Email);
        var dueDate = DateTime.UtcNow.AddDays(7).ToDateOnly();

        var activity = await teacherClient.CreateClassActivity(
            @class.Id,
            title: "Modelagem de Banco de Dados",
            description: "Modele um banco de dados para um sistema de gerenciamento de biblioteca.",
            type: ClassActivityType.Work,
            dueDate: dueDate).Success();

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await director.GetWebhookCalls(subscription.Id).Success();
        var call = await director.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventType.Should().Be(WebhookEventType.ClassActivityPublished);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.Subscription.Id.Should().Be(subscription.Id);

        using var payload = JsonDocument.Parse(call.Payload);
        payload.RootElement.GetProperty("event_id").GetString().Should().Be(call.EventUid);
        payload.RootElement.GetProperty("event_type").GetString().Should().Be(nameof(WebhookEventType.ClassActivityPublished));

        var data = payload.RootElement.GetProperty("data");
        data.GetProperty("id").GetInt32().Should().Be(activity.Id);
        data.GetProperty("class_id").GetInt32().Should().Be(@class.Id);
        data.GetProperty("title").GetString().Should().Be("Modelagem de Banco de Dados");
        data.GetProperty("type").GetString().Should().Be(nameof(ClassActivityType.Work));
        data.GetProperty("due_date").GetString().Should().Be(dueDate.ToString("yyyy-MM-dd"));

        var attempt = call.Attempts.Single();
        attempt.Status.Should().Be(WebhookCallAttemptStatus.Success);
        attempt.StatusCode.Should().Be(200);
        attempt.Response.Should().Contain("secret-key-123");
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_not_deliver_class_activity_webhook_when_subscription_does_not_have_the_event()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var subscription = await director.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.ShortcutGetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var teacherClient = await _back.LoginAs(teacher.Email);
        await teacherClient.CreateClassActivity(@class.Id).Success();

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await director.GetWebhookCalls(subscription.Id).Success();
        calls.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_only_to_the_subscription_listening_to_the_event()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscribed = await client.CreateWebhookSubscription(
            name: "Aluno criado",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        var notSubscribed = await client.CreateWebhookSubscription(
            name: "Atividade publicada",
            url: $"{FakesFactory.Url}/webhooks/target",
            events: [WebhookEventType.ClassActivityPublished]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await client.GetWebhookCalls(subscribed.Id).Success();
        var call = await client.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventType.Should().Be(WebhookEventType.StudentCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.Subscription.Id.Should().Be(subscribed.Id);

        var notSubscribedCalls = await client.GetWebhookCalls(notSubscribed.Id).Success();
        notSubscribedCalls.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_register_error_when_target_is_unreachable()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target/unreachable",
            events: [WebhookEventType.StudentCreated]).Success();

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
        attempt.StatusCode.Should().Be(999);
        attempt.Response.Should().NotBeEmpty();
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_respect_subscription_retry_configs()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscription = await client.CreateWebhookSubscription(
            url: $"{FakesFactory.Url}/webhooks/target/error",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.UpdateWebhookSubscriptionRetryConfigs(
            subscription.Id,
            maxRetries: 2,
            baseDelaySeconds: 2,
            backoffStrategy: BackoffStrategy.Linear).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();

        var callId = 0;
        var timeout = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < timeout)
        {
            await _back.AwaitCommandsProcessing();

            var calls = await client.GetWebhookCalls(subscription.Id).Success();
            if (calls.Items.Count == 1 && calls.Items[0].AttemptsCount >= 3)
            {
                callId = calls.Items[0].Id;
                break;
            }

            await Task.Delay(500);
        }

        // The retry chain is exhausted at this point: a further round must not create a 4th attempt
        await Task.Delay(TimeSpan.FromSeconds(7));
        await _back.AwaitCommandsProcessing();

        // Assert
        callId.Should().NotBe(0, "the webhook should have been attempted 3 times (1 call + 2 retries)");

        var call = await client.GetWebhookCall(callId).Success();
        call.Status.Should().Be(WebhookCallStatus.Error);
        call.AttemptsCount.Should().Be(3);
        call.Attempts.Should().HaveCount(3).And.OnlyContain(x => x.Status == WebhookCallAttemptStatus.Error);

        var attempts = call.Attempts.OrderBy(x => x.CreatedAt).ToList();
        (attempts[1].CreatedAt - attempts[0].CreatedAt).Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(2));
        (attempts[2].CreatedAt - attempts[1].CreatedAt).Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(4));
    }

    #endregion
}

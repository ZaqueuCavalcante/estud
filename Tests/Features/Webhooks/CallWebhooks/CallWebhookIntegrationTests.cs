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

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_when_class_activity_is_created()
    {
        // Arrange
        var director = await _back.LoggedAsDirector();

        var subscription = await director.CreateWebhookSubscription(
            name: "Atividade publicada",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.ClassActivityCreated],
            customHeaders: new() { ["X-Api-Key"] = "secret-key-123" }).Success();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
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
        var calls = await director.GetWebhookCalls().Success();
        var call = await director.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventType.Should().Be(WebhookEventType.ClassActivityCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.Subscription.Id.Should().Be(subscription.Id);

        using var payload = JsonDocument.Parse(call.Payload);
        payload.RootElement.GetProperty("Id").GetString().Should().Be(call.Uid);
        payload.RootElement.GetProperty("EventType").GetString().Should().Be(nameof(WebhookEventType.ClassActivityCreated));

        var data = payload.RootElement.GetProperty("Data");
        data.GetProperty("Id").GetInt32().Should().Be(activity.Id);
        data.GetProperty("ClassId").GetInt32().Should().Be(@class.Id);
        data.GetProperty("Title").GetString().Should().Be("Modelagem de Banco de Dados");
        data.GetProperty("Description").GetString().Should().Be("Modele um banco de dados para um sistema de gerenciamento de biblioteca.");
        data.GetProperty("Type").GetString().Should().Be(nameof(ClassActivityType.Work));
        data.GetProperty("DueDate").GetString().Should().Be(dueDate.ToString("yyyy-MM-dd"));

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

        await director.CreateWebhookSubscription(
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        var teacher = await director.CreateTeacher(DataGen.UserName, DataGen.Email).Success();
        var discipline = await director.CreateDiscipline().Success();
        await director.AssignDisciplinesToTeacher(teacher.Id, [discipline.Id]);

        var period = await director.GetFirstAcademicPeriod();
        var @class = await director.CreateClass(discipline.Id, period.Id).Success();
        await director.UpdateClassTeachers(@class.Id, [teacher.Id]);

        var teacherClient = await _back.LoginAs(teacher.Email);
        await teacherClient.CreateClassActivity(@class.Id).Success();

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await director.GetWebhookCalls().Success();
        calls.Items.Should().BeEmpty();
    }

    [Test]
    public async Task Webhooks_CallWebhook_Should_deliver_webhook_only_to_the_subscription_listening_to_the_event()
    {
        // Arrange
        var client = await _back.LoggedAsDirector();

        var subscribed = await client.CreateWebhookSubscription(
            name: "Aluno criado",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.StudentCreated]).Success();

        await client.CreateWebhookSubscription(
            name: "Atividade publicada",
            url: $"{MocksFactory.Url}/webhooks/target",
            events: [WebhookEventType.ClassActivityCreated]).Success();

        await client.CreateStudent(DataGen.UserName, DataGen.Email);

        // Act
        await _back.AwaitDomainEventsProcessing();
        await _back.AwaitCommandsProcessing();

        // Assert
        var calls = await client.GetWebhookCalls().Success();
        var call = await client.GetWebhookCall(calls.Items.Single().Id).Success();

        call.EventType.Should().Be(WebhookEventType.StudentCreated);
        call.Status.Should().Be(WebhookCallStatus.Success);
        call.Subscription.Id.Should().Be(subscribed.Id);
    }

    #endregion
}

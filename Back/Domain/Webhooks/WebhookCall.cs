namespace Estud.Back.Domain.Webhooks;

/// <summary>
/// Chamada de Webhook
/// </summary>
public class WebhookCall : DomainEntity
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public int WebhookSubscriptionId { get; set; }
    public string Payload { get; set; }
    public WebhookEventType EventType { get; set; }
    public WebhookCallStatus Status { get; set; }
    public int AttemptsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<WebhookCallAttempt> Attempts { get; set; }
    public WebhookSubscription Subscription { get; set; }

    private WebhookCall() { }

    public WebhookCall(
        int institutionId,
        int webhookSubscriptionId,
        object data,
        WebhookEventType eventType)
    {
        InstitutionId = institutionId;
        WebhookSubscriptionId = webhookSubscriptionId;
        EventType = eventType;
        CreatedAt = DateTime.UtcNow;
        Status = WebhookCallStatus.Pending;
        Payload = (new { Id = Uid, EventType = eventType, OccurredAt = CreatedAt, Data = data }).Serialize();
    }

    public void Success(int statusCode, string response, int durationMs)
    {
        AttemptsCount++;
        Status = WebhookCallStatus.Success;
        Attempts.Add(new WebhookCallAttempt(Id, WebhookCallAttemptStatus.Success, statusCode, response, durationMs));
    }

    public void Failed(int statusCode, string response, int durationMs)
    {
        Status = WebhookCallStatus.Error;
        Attempts.Add(new WebhookCallAttempt(Id, WebhookCallAttemptStatus.Error, statusCode, response, durationMs));

        AttemptsCount++;
    }
}

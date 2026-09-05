namespace Estud.Back.Features.Webhooks.GetWebhookCall;

public class GetWebhookCallOut : IApiDto<GetWebhookCallOut>
{
    public int Id { get; set; }
    public WebhookEventType EventType { get; set; }
    public WebhookCallStatus Status { get; set; }
    public int AttemptsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Payload { get; set; }
    public GetWebhookCallRequestOut Request { get; set; }
    public GetWebhookCallSubscriptionOut Subscription { get; set; }
    public List<GetWebhookCallAttemptOut> Attempts { get; set; } = [];

    public static IEnumerable<(string, GetWebhookCallOut)> GetExamples() =>
    [
        ("Exemplo", new GetWebhookCallOut
        {
            Id = 1,
            EventType = WebhookEventType.StudentCreated,
            Status = WebhookCallStatus.Success,
            AttemptsCount = 1,
            CreatedAt = DateTime.UtcNow,
            Payload = """{"EventType":"StudentCreated","Data":{"Id":1,"Name":"João da Silva"}}""",
            Request = new GetWebhookCallRequestOut
            {
                Method = "POST",
                Url = "https://webhook.site/my-webhook",
                Headers = new()
                {
                    ["Estud-AuthToken"] = "6r4g654rs6g4we6f4qw684f68qwf4",
                    ["Content-Type"] = "application/json; charset=utf-8",
                },
                Body = """{"EventType":"StudentCreated","Data":{"Id":1,"Name":"João da Silva"}}""",
            },
            Subscription = new GetWebhookCallSubscriptionOut
            {
                Id = 1,
                Name = "Aluno criado",
                Url = "https://webhook.site/my-webhook",
                IsActive = true,
            },
            Attempts =
            [
                new GetWebhookCallAttemptOut
                {
                    Id = 1,
                    Status = WebhookCallAttemptStatus.Success,
                    StatusCode = 200,
                    Response = """{"ok":true}""",
                    DurationMs = 143,
                    CreatedAt = DateTime.UtcNow,
                },
            ],
        }),
    ];
}

public class GetWebhookCallRequestOut
{
    public string Method { get; set; }
    public string Url { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string Body { get; set; }
}

public class GetWebhookCallSubscriptionOut
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public bool IsActive { get; set; }
}

public class GetWebhookCallAttemptOut
{
    public int Id { get; set; }
    public WebhookCallAttemptStatus Status { get; set; }
    public int StatusCode { get; set; }
    public string Response { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

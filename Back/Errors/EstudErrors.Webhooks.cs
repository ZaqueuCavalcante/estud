namespace Estud.Back.Errors;

public class WebhookSubscriptionNotFound : EstudError
{
    public static readonly WebhookSubscriptionNotFound I = new();
    public override string Code { get; set; } = nameof(WebhookSubscriptionNotFound);
    public override string Message { get; set; } = "Inscrição de webhook não encontrada.";
}

public class WebhookCallNotFound : EstudError
{
    public static readonly WebhookCallNotFound I = new();
    public override string Code { get; set; } = nameof(WebhookCallNotFound);
    public override string Message { get; set; } = "Chamada de webhook não encontrada.";
}

public class WebhookCallCannotBeRetried : EstudError
{
    public static readonly WebhookCallCannotBeRetried I = new();
    public override string Code { get; set; } = nameof(WebhookCallCannotBeRetried);
    public override string Message { get; set; } = "Apenas chamadas de webhook com erro podem ser reprocessadas.";
}

public class WebhookCallFailed : EstudError
{
    public static readonly WebhookCallFailed I = new();
    public override string Code { get; set; } = nameof(WebhookCallFailed);
    public override string Message { get; set; } = "Falha ao chamar o webhook.";
}

public class InvalidWebhookName : EstudError
{
    public static readonly InvalidWebhookName I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookName);
    public override string Message { get; set; } = "Nome de webhook inválido.";
}

public class InvalidWebhookUrl : EstudError
{
    public static readonly InvalidWebhookUrl I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookUrl);
    public override string Message { get; set; } = "URL de webhook inválida.";
}

public class InvalidWebhookEvents : EstudError
{
    public static readonly InvalidWebhookEvents I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookEvents);
    public override string Message { get; set; } = "Lista de eventos de webhook inválida.";
}

public class InvalidWebhookCustomHeaders : EstudError
{
    public static readonly InvalidWebhookCustomHeaders I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookCustomHeaders);
    public override string Message { get; set; } = "Headers customizados de webhook inválidos.";
}

public class InvalidWebhookMaxRetries : EstudError
{
    public static readonly InvalidWebhookMaxRetries I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookMaxRetries);
    public override string Message { get; set; } = "Número máximo de retentativas de webhook inválido. Deve estar entre 0 e 5.";
}

public class InvalidWebhookBaseDelaySeconds : EstudError
{
    public static readonly InvalidWebhookBaseDelaySeconds I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookBaseDelaySeconds);
    public override string Message { get; set; } = "Intervalo base de retentativas de webhook inválido. Deve estar entre 0 e 1800 segundos (30 minutos).";
}

public class InvalidWebhookBackoffStrategy : EstudError
{
    public static readonly InvalidWebhookBackoffStrategy I = new();
    public override string Code { get; set; } = nameof(InvalidWebhookBackoffStrategy);
    public override string Message { get; set; } = "Estratégia de backoff de webhook inválida.";
}

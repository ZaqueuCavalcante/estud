namespace Estud.Back.Domain.Enums;

/// <summary>
/// Origem do evento de webhook
/// </summary>
public enum ReceivedWebhookEventSource
{
    [Description("Stripe")]
    Stripe = 0,
}

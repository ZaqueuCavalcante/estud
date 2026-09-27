namespace Estud.Back.Domain.Enums;

/// <summary>
/// Eventos que podem ser enviados via Webhook
/// </summary>
public enum WebhookEventType
{
    [Description("Aluno criado")]
    StudentCreated = 0,

    [Description("Professor criado")]
    TeacherCreated = 100,

    [Description("Atividade publicada")]
    ClassActivityPublished = 200,
}

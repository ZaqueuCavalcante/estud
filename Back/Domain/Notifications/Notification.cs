using System.Text.Json;

namespace Estud.Back.Domain.Notifications;

public class Notification
{
    public int Id { get; set; }
    public int InstitutionId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public JsonDocument? Metadata { get; set; }

    public Notification() { }

    public Notification(
        int institutionId,
        NotificationType notificationType,
        string title,
        string description,
        object? metadata = null)
        : this(notificationType, title, description, metadata)
    {
        InstitutionId = institutionId;
    }

    private Notification(
        NotificationType notificationType,
        string title,
        string description,
        object? metadata)
    {
        NotificationType = notificationType;
        Title = title;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        Metadata = metadata != null ? JsonDocument.Parse(metadata.Serialize()) : null;
    }

    public static Notification Welcome()
    {
        return new Notification(
            NotificationType.Welcome,
            "Boas-vindas ao Estud!",
            "Obrigado por confiar no Estud para gerir sua instituição. Configure seu perfil e siga a documentação para dar os primeiros passos.",
            new
            {
                links = new[]
                {
                    new { label = "Primeiros passos", to = "/docs", icon = "i-lucide-book-open", newTab = true },
                    new { label = "Configurar instituição", to = "/configs", icon = "i-lucide-settings", newTab = false },
                    new { label = "Completar perfil", to = "/account", icon = "i-lucide-user", newTab = false },
                },
            }
        );
    }
}

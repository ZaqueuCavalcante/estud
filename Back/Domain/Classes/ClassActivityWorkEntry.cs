using System.Text.Json;
using Estud.Back.Domain.Identity;

namespace Estud.Back.Domain.Classes;

/// <summary>
/// Item da linha do tempo de uma entrega de atividade.
/// </summary>
public class ClassActivityWorkEntry : DomainEntity
{
    public int Id { get; set; }
    public int ClassActivityWorkId { get; set; }
    public int UserId { get; set; }
    public EstudUser? User { get; set; }
    public ClassActivityWorkEntryType Type { get; set; }
    public string? Content { get; set; }
    public JsonDocument? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }

    private ClassActivityWorkEntry() {}

    public ClassActivityWorkEntry(
        int classActivityWorkId,
        int userId,
        ClassActivityWorkEntryType type,
        string? content = null,
        object? metadata = null)
    {
        ClassActivityWorkId = classActivityWorkId;
        UserId = userId;
        Type = type;
        Content = content;
        Metadata = metadata != null ? JsonDocument.Parse(metadata.SerializeAsCamelCase()) : null;
        CreatedAt = DateTime.UtcNow;
    }
}

public class ClassActivityWorkNoteChange
{
    public decimal FromNote { get; set; }
    public decimal ToNote { get; set; }
}

public class ClassActivityWorkStatusChange
{
    public ClassActivityWorkStatus FromStatus { get; set; }
    public ClassActivityWorkStatus ToStatus { get; set; }
}

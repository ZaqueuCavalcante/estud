namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma Aula
/// </summary>
public enum ClassLessonStatus
{
    [Description("Pendente")]
    Pending,

    [Description("Concluída")]
    Finalized,
}

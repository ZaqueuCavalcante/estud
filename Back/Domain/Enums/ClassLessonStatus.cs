namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma Aula
/// </summary>
public enum ClassLessonStatus
{
    [Description("Pendente")]
    Pending = 0,

    [Description("Concluída")]
    Finalized = 1,
}

namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma Atividade
/// </summary>
public enum ClassActivityStatus
{
    [Description("Pendente")]
    Pending = 0,

    [Description("Publicada")]
    Published = 1,

    [Description("Finalizada")]
    Finalized = 2,
}

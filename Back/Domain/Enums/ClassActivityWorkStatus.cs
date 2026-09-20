namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma entrega de atividade
/// </summary>
public enum ClassActivityWorkStatus
{
    [Description("Pendente")]
    Pending = 0,

    [Description("Correção")]
    Review = 1,

    [Description("Finalizada")]
    Finalized = 2,
}

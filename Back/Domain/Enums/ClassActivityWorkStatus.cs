namespace Estud.Back.Domain.Enums;

/// <summary>
/// Status de uma entrega de atividade
/// </summary>
public enum ClassActivityWorkStatus
{
    [Description("Pendente")]
    Pending = 0,

    [Description("Entregue")]
    Delivered = 1,

    [Description("Finalizado")]
    Finalized = 2,
}

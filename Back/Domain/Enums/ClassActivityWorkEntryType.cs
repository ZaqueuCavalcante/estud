namespace Estud.Back.Domain.Enums;

/// <summary>
/// Tipo de item da linha do tempo de uma entrega
/// </summary>
public enum ClassActivityWorkEntryType
{
    [Description("Comentário")]
    Comment = 0,

    [Description("Alteração de Nota")]
    NoteChange = 1,

    [Description("Alteração de Status")]
    StatusChange = 2,
}

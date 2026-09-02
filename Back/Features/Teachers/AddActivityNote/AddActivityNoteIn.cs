namespace Estud.Back.Features.Teachers.AddActivityNote;

public class AddActivityNoteIn : IApiDto<AddActivityNoteIn>
{
    /// <summary>
    /// Nota da entrega, no intervalo: 0 ≤ Note ≤ 10
    /// </summary>
    public decimal Note { get; set; }

    public static IEnumerable<(string, AddActivityNoteIn)> GetExamples() =>
    [
        ("Exemplo", new AddActivityNoteIn { Note = 8.5m }),
    ];
}

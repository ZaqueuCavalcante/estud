namespace Estud.Back.Features.Teachers.CreateClassActivityWorkEntry;

public class CreateClassActivityWorkEntryIn : IApiDto<CreateClassActivityWorkEntryIn>
{
    /// <summary>
    /// Comentário do professor em markdown (texto, links, imagens e PDFs enviados para o storage)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Nova nota da entrega, no intervalo: 0 ≤ Note ≤ 10
    /// </summary>
    public decimal? Note { get; set; }

    /// <summary>
    /// Novo status da entrega
    /// </summary>
    public ClassActivityWorkStatus? Status { get; set; }

    public static IEnumerable<(string, CreateClassActivityWorkEntryIn)> GetExamples() =>
    [
        ("Comentário", new CreateClassActivityWorkEntryIn
        {
            Content = "O diagrama não tem as cardinalidades entre livro e autor.",
        }),
        ("Nota", new CreateClassActivityWorkEntryIn { Note = 7.5M }),
        ("Nota e finalização", new CreateClassActivityWorkEntryIn
        {
            Content = "Ficou ótimo depois dos ajustes.",
            Note = 8.5M,
            Status = ClassActivityWorkStatus.Finalized,
        }),
    ];
}

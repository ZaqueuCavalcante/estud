namespace Estud.Back.Features.Students.CreateClassActivityWorkFile;

public class CreateClassActivityWorkFileIn : IApiDto<CreateClassActivityWorkFileIn>
{
    /// <summary>
    /// Content-Type do arquivo (image/png, image/jpeg, image/webp ou application/pdf)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Tamanho do arquivo em bytes (máximo de 5 MB para imagens e 10 MB para PDFs)
    /// </summary>
    public long SizeInBytes { get; set; }

    public static IEnumerable<(string, CreateClassActivityWorkFileIn)> GetExamples() =>
    [
        ("Imagem", new CreateClassActivityWorkFileIn { ContentType = "image/png", SizeInBytes = 245_000 }),
        ("PDF", new CreateClassActivityWorkFileIn { ContentType = "application/pdf", SizeInBytes = 1_850_000 }),
    ];
}

namespace Estud.Back.Features.Teachers.CreateClassActivityFile;

public class CreateClassActivityFileIn : IApiDto<CreateClassActivityFileIn>
{
    /// <summary>
    /// Content-Type do arquivo (image/png, image/jpeg, image/webp ou application/pdf)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Tamanho do arquivo em bytes (máximo de 5 MB para imagens e 10 MB para PDFs)
    /// </summary>
    public long SizeInBytes { get; set; }

    public static IEnumerable<(string, CreateClassActivityFileIn)> GetExamples() =>
    [
        ("Imagem", new CreateClassActivityFileIn { ContentType = "image/png", SizeInBytes = 245_000 }),
        ("PDF", new CreateClassActivityFileIn { ContentType = "application/pdf", SizeInBytes = 1_850_000 }),
    ];
}

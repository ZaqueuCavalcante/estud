namespace Estud.Back.Features.Teachers.CreateLessonPlanImage;

public class CreateLessonPlanImageIn : IApiDto<CreateLessonPlanImageIn>
{
    /// <summary>
    /// Content-Type da imagem (image/png, image/jpeg ou image/webp)
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Tamanho da imagem em bytes (máximo de 5 MB)
    /// </summary>
    public long SizeInBytes { get; set; }

    public static IEnumerable<(string, CreateLessonPlanImageIn)> GetExamples() =>
    [
        ("Exemplo", new CreateLessonPlanImageIn { ContentType = "image/png", SizeInBytes = 245_000 }),
    ];
}

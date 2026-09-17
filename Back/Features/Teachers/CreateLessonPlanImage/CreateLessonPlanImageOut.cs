namespace Estud.Back.Features.Teachers.CreateLessonPlanImage;

public class CreateLessonPlanImageOut : IApiDto<CreateLessonPlanImageOut>
{
    /// <summary>
    /// URL pré-assinada para enviar a imagem via PUT
    /// </summary>
    public string UploadUrl { get; set; }

    /// <summary>
    /// URL pública da imagem, para referenciar no planejamento
    /// </summary>
    public string PublicUrl { get; set; }

    public static IEnumerable<(string, CreateLessonPlanImageOut)> GetExamples() =>
    [
        ("Exemplo", new CreateLessonPlanImageOut
        {
            UploadUrl = "https://<account-id>.r2.cloudflarestorage.com/estud-files/lesson-plan-images/1/2/3/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png?X-Amz-Signature=...",
            PublicUrl = "https://cdn.estud.com.br/lesson-plan-images/1/2/3/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png",
        }),
    ];
}

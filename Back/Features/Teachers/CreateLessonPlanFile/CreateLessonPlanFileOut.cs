namespace Estud.Back.Features.Teachers.CreateLessonPlanFile;

public class CreateLessonPlanFileOut : IApiDto<CreateLessonPlanFileOut>
{
    /// <summary>
    /// URL pré-assinada para enviar o arquivo via PUT
    /// </summary>
    public string UploadUrl { get; set; }

    /// <summary>
    /// URL pública do arquivo, para referenciar no planejamento
    /// </summary>
    public string PublicUrl { get; set; }

    public static IEnumerable<(string, CreateLessonPlanFileOut)> GetExamples() =>
    [
        ("Exemplo", new CreateLessonPlanFileOut
        {
            UploadUrl = "https://<account-id>.r2.cloudflarestorage.com/estud-files/lesson-plan-files/1/2/3/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png?X-Amz-Signature=...",
            PublicUrl = "https://cdn.estud.com.br/lesson-plan-files/1/2/3/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png",
        }),
    ];
}

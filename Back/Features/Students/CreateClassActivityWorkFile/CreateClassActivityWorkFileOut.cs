namespace Estud.Back.Features.Students.CreateClassActivityWorkFile;

public class CreateClassActivityWorkFileOut : IApiDto<CreateClassActivityWorkFileOut>
{
    /// <summary>
    /// URL pré-assinada para enviar o arquivo via PUT
    /// </summary>
    public string UploadUrl { get; set; }

    /// <summary>
    /// URL pública do arquivo, para referenciar na entrega
    /// </summary>
    public string PublicUrl { get; set; }

    public static IEnumerable<(string, CreateClassActivityWorkFileOut)> GetExamples() =>
    [
        ("Exemplo", new CreateClassActivityWorkFileOut
        {
            UploadUrl = "https://<account-id>.r2.cloudflarestorage.com/estud-files/class-activity-work-files/1/2/3/4/01K5B4Z8Q2M3N4P5R6S7T8V9W0.pdf?X-Amz-Signature=...",
            PublicUrl = "https://cdn.estud.com.br/class-activity-work-files/1/2/3/4/01K5B4Z8Q2M3N4P5R6S7T8V9W0.pdf",
        }),
    ];
}

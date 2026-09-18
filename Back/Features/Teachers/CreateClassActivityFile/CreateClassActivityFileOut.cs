namespace Estud.Back.Features.Teachers.CreateClassActivityFile;

public class CreateClassActivityFileOut : IApiDto<CreateClassActivityFileOut>
{
    /// <summary>
    /// URL pré-assinada para enviar o arquivo via PUT
    /// </summary>
    public string UploadUrl { get; set; }

    /// <summary>
    /// URL pública do arquivo, para referenciar na descrição da atividade
    /// </summary>
    public string PublicUrl { get; set; }

    public static IEnumerable<(string, CreateClassActivityFileOut)> GetExamples() =>
    [
        ("Exemplo", new CreateClassActivityFileOut
        {
            UploadUrl = "https://<account-id>.r2.cloudflarestorage.com/estud-files/class-activity-files/1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.pdf?X-Amz-Signature=...",
            PublicUrl = "https://cdn.estud.com.br/class-activity-files/1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.pdf",
        }),
    ];
}

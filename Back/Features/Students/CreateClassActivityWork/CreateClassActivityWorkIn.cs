namespace Estud.Back.Features.Students.CreateClassActivityWork;

public class CreateClassActivityWorkIn : IApiDto<CreateClassActivityWorkIn>
{
    /// <summary>
    /// Conteúdo da entrega em markdown (texto, links, imagens e PDFs enviados para o storage)
    /// </summary>
    public string? Content { get; set; }

    public static IEnumerable<(string, CreateClassActivityWorkIn)> GetExamples() =>
    [
        ("Exemplo", new CreateClassActivityWorkIn
        {
            Content = "Segue a modelagem do banco da biblioteca.\n\n![Diagrama ER](https://cdn.estud.com.br/class-activity-work-files/1/2/3/4/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png)",
        }),
    ];
}

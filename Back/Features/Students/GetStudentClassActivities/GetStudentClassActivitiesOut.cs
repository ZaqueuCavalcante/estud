namespace Estud.Back.Features.Students.GetStudentClassActivities;

public class GetStudentClassActivitiesOut : IApiDto<GetStudentClassActivitiesOut>
{
    public List<GetStudentClassActivitiesNoteOut> Notes { get; set; } = [];
    public List<GetStudentClassActivitiesItemOut> Activities { get; set; } = [];

    public static IEnumerable<(string, GetStudentClassActivitiesOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentClassActivitiesOut
        {
            Notes =
            [
                new() { Note = ClassNoteType.N1, Performance = 85.0M },
                new() { Note = ClassNoteType.N2, Performance = null },
                new() { Note = ClassNoteType.N3, Performance = null },
            ],
            Activities =
            [
                new()
                {
                    Id = 1,
                    ClassId = 1,
                    Note = ClassNoteType.N1,
                    Title = "Trabalho de Grafos",
                    Description = "Implementar o algoritmo de Dijkstra.",
                    Type = ClassActivityType.Work,
                    Status = ClassActivityStatus.Published,
                    Weight = 25,
                    CreatedAt = new DateTime(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc),
                    DueDate = new DateOnly(2026, 3, 20),
                    DueHour = Hour.H22_00,
                    WorkStatus = ClassActivityWorkStatus.Finalized,
                    WorkLink = "https://github.com/ZaqueuCavalcante/estud",
                    Value = 8.5M,
                    PonderedValue = 2.125M,
                },
            ],
        }),
    ];
}

public class GetStudentClassActivitiesNoteOut
{
    public ClassNoteType Note { get; set; }

    /// <summary>
    /// Aproveitamento do aluno logado na nota (de 0% a 100%): pontos ganhos sobre os pontos
    /// de todas as atividades já criadas na nota. Atividade sem nota conta como 0.
    /// Nulo enquanto a nota não tem atividade.
    /// </summary>
    public decimal? Performance { get; set; }
}

public class GetStudentClassActivitiesItemOut
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public ClassNoteType Note { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ClassActivityType Type { get; set; }
    public ClassActivityStatus Status { get; set; }
    public int Weight { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateOnly DueDate { get; set; }
    public Hour DueHour { get; set; }

    /// <summary>
    /// Status da entrega do aluno logado
    /// </summary>
    public ClassActivityWorkStatus WorkStatus { get; set; }

    /// <summary>
    /// Link entregue pelo aluno logado
    /// </summary>
    public string? WorkLink { get; set; }

    /// <summary>
    /// Nota do aluno logado na atividade
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Nota do aluno logado ponderada pelo peso da atividade
    /// </summary>
    public decimal PonderedValue { get; set; }
}

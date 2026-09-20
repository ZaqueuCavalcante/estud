using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Students.GetStudentClassActivity;

public class GetStudentClassActivityOut : IApiDto<GetStudentClassActivityOut>
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
    /// Linha do tempo da entrega do aluno logado, em ordem cronológica
    /// </summary>
    public List<GetStudentClassActivityWorkEntryOut> WorkEntries { get; set; } = [];

    /// <summary>
    /// Nota do aluno logado na atividade
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Nota do aluno logado ponderada pelo peso da atividade
    /// </summary>
    public decimal PonderedValue { get; set; }

    public static IEnumerable<(string, GetStudentClassActivityOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentClassActivityOut
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
            WorkStatus = ClassActivityWorkStatus.Review,
            WorkEntries =
            [
                new()
                {
                    Id = 1,
                    UserId = 4,
                    User = "Maria Souza",
                    Type = ClassActivityWorkEntryType.Comment,
                    Content = "Segue a modelagem do banco da biblioteca.\n\n![Diagrama ER](https://cdn.estud.com.br/class-activity-work-files/1/2/3/4/01K5B4Z8Q2M3N4P5R6S7T8V9W0.png)",
                    CreatedAt = new DateTime(2026, 3, 18, 22, 14, 0, DateTimeKind.Utc),
                },
                new()
                {
                    Id = 2,
                    UserId = 7,
                    User = "João Alves",
                    Type = ClassActivityWorkEntryType.NoteChange,
                    Metadata = new ClassActivityWorkNoteChange { FromNote = 0, ToNote = 8.5M },
                    CreatedAt = new DateTime(2026, 3, 19, 9, 2, 0, DateTimeKind.Utc),
                },
            ],
            Value = 8.5M,
            PonderedValue = 2.125M,
        }),
    ];
}

public class GetStudentClassActivityWorkEntryOut
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Nome de quem criou o item
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Foto de perfil de quem criou o item
    /// </summary>
    public string? UserPhoto { get; set; }

    public ClassActivityWorkEntryType Type { get; set; }

    /// <summary>
    /// Conteúdo (markdown) do comentário
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Dados da mudança de nota ({ FromNote, ToNote }) ou de status ({ FromStatus, ToStatus })
    /// </summary>
    public object? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
}

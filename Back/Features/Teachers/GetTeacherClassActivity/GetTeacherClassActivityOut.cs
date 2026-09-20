using Estud.Back.Domain.Classes;

namespace Estud.Back.Features.Teachers.GetTeacherClassActivity;

public class GetTeacherClassActivityOut : IApiDto<GetTeacherClassActivityOut>
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
    /// Total de entregas feitas pelos alunos
    /// </summary>
    public int DeliveredWorks { get; set; }

    /// <summary>
    /// Total de entregas esperadas na atividade
    /// </summary>
    public int TotalWorks { get; set; }

    /// <summary>
    /// Entregas dos alunos matriculados na turma
    /// </summary>
    public List<GetTeacherClassActivityWorkOut> Works { get; set; } = [];

    public static IEnumerable<(string, GetTeacherClassActivityOut)> GetExamples() =>
    [
        ("Exemplo", new GetTeacherClassActivityOut
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
            DeliveredWorks = 1,
            TotalWorks = 2,
            Works =
            [
                new()
                {
                    Id = 1,
                    StudentId = 1,
                    Student = "Maria Souza",
                    Status = ClassActivityWorkStatus.Review,
                    Value = 0,
                    Entries =
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
                            Metadata = new ClassActivityWorkNoteChange { FromNote = 0, ToNote = 7.5M },
                            CreatedAt = new DateTime(2026, 3, 19, 9, 2, 0, DateTimeKind.Utc),
                        },
                    ],
                },
                new()
                {
                    Id = 2,
                    StudentId = 2,
                    Student = "Chico Ferreira",
                    Status = ClassActivityWorkStatus.Pending,
                    Value = 0,
                },
            ],
        }),
    ];
}

public class GetTeacherClassActivityWorkOut
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string Student { get; set; }

    /// <summary>
    /// Foto de perfil do aluno
    /// </summary>
    public string? StudentPhoto { get; set; }

    public ClassActivityWorkStatus Status { get; set; }

    /// <summary>
    /// Nota do aluno na atividade
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Linha do tempo da entrega, em ordem cronológica
    /// </summary>
    public List<GetTeacherClassActivityWorkEntryOut> Entries { get; set; } = [];
}

public class GetTeacherClassActivityWorkEntryOut
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

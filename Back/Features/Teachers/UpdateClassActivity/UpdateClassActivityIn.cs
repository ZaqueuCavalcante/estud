namespace Estud.Back.Features.Teachers.UpdateClassActivity;

public class UpdateClassActivityIn : IApiDto<UpdateClassActivityIn>
{
    /// <summary>
    /// Nota
    /// </summary>
    public ClassNoteType Note { get; set; }

    /// <summary>
    /// Título
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Descrição
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Tipo
    /// </summary>
    public ClassActivityType Type { get; set; }

    /// <summary>
    /// Peso no intervalo: 0 ≤ Weight ≤ 100
    /// </summary>
    public int Weight { get; set; }

    /// <summary>
    /// Data limite para entrega
    /// </summary>
    public DateOnly DueDate { get; set; }

    /// <summary>
    /// Hora limite para entrega
    /// </summary>
    public Hour DueHour { get; set; }

    /// <summary>
    /// Notificar os alunos da turma sobre a alteração
    /// </summary>
    public bool NotifyStudents { get; set; }

    public static IEnumerable<(string, UpdateClassActivityIn)> GetExamples() =>
    [
        ("Modelagem de Banco de Dados",
        new UpdateClassActivityIn
        {
            Note = ClassNoteType.N1,
            Title = "Modelagem de Banco de Dados",
            Description = "Modele um banco de dados para um sistema de gerenciamento de biblioteca, com pelo menos 5 entidades.",
            Type = ClassActivityType.Work,
            Weight = 40,
            DueDate = DateTime.UtcNow.AddDays(10).ToDateOnly(),
            DueHour = Hour.H23_00,
            NotifyStudents = true,
        }),
    ];
}

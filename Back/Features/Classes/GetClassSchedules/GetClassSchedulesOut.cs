namespace Estud.Back.Features.Classes.GetClassSchedules;

public class GetClassSchedulesOut : IApiDto<GetClassSchedulesOut>
{
    public List<GetClassSchedulesItemOut> Schedules { get; set; } = [];

    public static IEnumerable<(string, GetClassSchedulesOut)> GetExamples() =>
    [
        ("Exemplo", new GetClassSchedulesOut
        {
            Schedules =
            [
                new()
                {
                    ClassId = 1, Discipline = "Banco de Dados", FromOtherClass = false,
                    Day = Day.Monday, StartAt = Hour.H07_00, EndAt = Hour.H10_00,
                    TeacherId = 14, Teacher = "Ana Lima", ClassroomId = 5, Classroom = "Sala 05",
                },
                new()
                {
                    ClassId = 7, Discipline = "Estruturas de Dados", FromOtherClass = true,
                    Day = Day.Wednesday, StartAt = Hour.H19_00, EndAt = Hour.H22_00,
                    TeacherId = 14, Teacher = "Ana Lima", ClassroomId = 3, Classroom = "Laboratório 03",
                },
            ],
        }),
    ];
}

public class GetClassSchedulesItemOut
{
    public int ClassId { get; set; }
    public string Discipline { get; set; }

    /// <summary>
    /// Verdadeiro quando o horário é de outra turma de um dos professores desta turma.
    /// </summary>
    public bool FromOtherClass { get; set; }

    public Day Day { get; set; }
    public Hour StartAt { get; set; }
    public Hour EndAt { get; set; }

    public int? TeacherId { get; set; }
    public string? Teacher { get; set; }

    public int? ClassroomId { get; set; }
    public string? Classroom { get; set; }
}

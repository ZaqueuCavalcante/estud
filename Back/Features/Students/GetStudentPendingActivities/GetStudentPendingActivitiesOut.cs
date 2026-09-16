namespace Estud.Back.Features.Students.GetStudentPendingActivities;

public class GetStudentPendingActivitiesOut : IApiDto<GetStudentPendingActivitiesOut>
{
    /// <summary>
    /// Total de atividades sem entrega do aluno logado.
    /// </summary>
    public int Total { get; set; }

    public static IEnumerable<(string, GetStudentPendingActivitiesOut)> GetExamples() =>
    [
        ("Exemplo", new GetStudentPendingActivitiesOut { Total = 3 }),
    ];
}

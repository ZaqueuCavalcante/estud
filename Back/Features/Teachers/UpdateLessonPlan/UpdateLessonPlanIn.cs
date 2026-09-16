namespace Estud.Back.Features.Teachers.UpdateLessonPlan;

public class UpdateLessonPlanIn : IApiDto<UpdateLessonPlanIn>
{
    /// <summary>
    /// Conteúdo planejado para a aula
    /// </summary>
    public string? PlannedContent { get; set; }

    public static IEnumerable<(string, UpdateLessonPlanIn)> GetExamples() =>
    [
        ("Exemplo", new UpdateLessonPlanIn
        {
            PlannedContent = "Introdução a grafos: definições, representação por matriz de adjacência e exercícios.",
        }),
        ("Limpar planejamento", new UpdateLessonPlanIn { PlannedContent = null }),
    ];
}

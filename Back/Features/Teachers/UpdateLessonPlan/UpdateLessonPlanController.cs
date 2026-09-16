namespace Estud.Back.Features.Teachers.UpdateLessonPlan;

[ApiController, Authorize(Policies.UpdateLessonPlan)]
public class UpdateLessonPlanController(UpdateLessonPlanService service) : ControllerBase
{
    /// <summary>
    /// Planejar aula
    /// </summary>
    /// <remarks>
    /// Grava o planejamento de uma aula, que fica visível para os alunos da turma.
    /// Apenas o professor da turma pode planejar a aula, em qualquer data.
    /// Enviar um texto vazio limpa o planejamento.
    /// </remarks>
    [HttpPut("teachers/lessons/{lessonId}/plan")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int lessonId, [FromBody] UpdateLessonPlanIn data)
    {
        var result = await service.Update(lessonId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateLessonPlanIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidClassLessonPlan,
    ClassLessonNotFound,
    TeacherNotAssignedToClass
>;

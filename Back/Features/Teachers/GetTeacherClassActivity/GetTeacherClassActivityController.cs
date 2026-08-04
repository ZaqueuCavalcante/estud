namespace Estud.Back.Features.Teachers.GetTeacherClassActivity;

[ApiController, Authorize(Policies.GetTeacherClassActivity)]
public class GetTeacherClassActivityController(GetTeacherClassActivityService service) : ControllerBase
{
    /// <summary>
    /// Atividade da turma
    /// </summary>
    /// <remarks>
    /// Retorna uma atividade de uma turma lecionada pelo professor logado,
    /// com as entregas de cada aluno matriculado.
    /// </remarks>
    [HttpGet("teachers/classes/{classId}/activities/{activityId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromRoute] int activityId)
    {
        var result = await service.Get(classId, activityId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetTeacherClassActivityOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound, TeacherNotAssignedToClass, ClassActivityNotFound>;

namespace Estud.Back.Features.Students.GetStudentClassActivity;

[ApiController, Authorize(Policies.GetStudentClassActivity)]
public class GetStudentClassActivityController(GetStudentClassActivityService service) : ControllerBase
{
    /// <summary>
    /// Atividade da turma
    /// </summary>
    /// <remarks>
    /// Retorna uma atividade de uma turma em que o aluno logado está matriculado,
    /// com o status e a nota da entrega dele na atividade.
    /// </remarks>
    [HttpGet("students/classes/{classId}/activities/{activityId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromRoute] int activityId)
    {
        var result = await service.Get(classId, activityId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetStudentClassActivityOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound, StudentNotEnrolledInClass, ClassActivityNotFound>;

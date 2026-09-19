namespace Estud.Back.Features.Teachers.UpdateClassActivity;

[ApiController, Authorize(Policies.UpdateClassActivity)]
public class UpdateClassActivityController(UpdateClassActivityService service) : ControllerBase
{
    /// <summary>
    /// Atualizar atividade
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de uma atividade de uma turma lecionada pelo professor logado.
    /// A soma dos pesos das atividades de uma mesma nota não pode passar de 100.
    /// Com NotifyStudents, os alunos da turma recebem uma notificação sobre a alteração.
    /// </remarks>
    [HttpPut("teachers/classes/{classId}/activities/{activityId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int classId, [FromRoute] int activityId, [FromBody] UpdateClassActivityIn data)
    {
        var result = await service.Update(classId, activityId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateClassActivityIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassNotFound,
    TeacherNotAssignedToClass,
    ClassActivityNotFound,
    NoteTypeNotUsedByInstitution,
    InvalidClassActivityWeight
>;

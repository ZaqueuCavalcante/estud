namespace Estud.Back.Features.Teachers.AddActivityNote;

[ApiController, Authorize(Policies.AddActivityNote)]
public class AddActivityNoteController(AddActivityNoteService service) : ControllerBase
{
    /// <summary>
    /// Adicionar nota em entrega
    /// </summary>
    /// <remarks>
    /// Adiciona a nota da entrega de um aluno numa atividade da turma, finalizando a entrega.
    /// Apenas o professor da turma pode dar a nota.
    /// </remarks>
    [HttpPut("teachers/activities/{activityId}/works/{workId}/note")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Add([FromRoute] int activityId, [FromRoute] int workId, [FromBody] AddActivityNoteIn data)
    {
        var result = await service.Add(activityId, workId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<AddActivityNoteIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassActivityNotFound,
    TeacherNotAssignedToClass,
    ClassActivityWorkNotFound,
    InvalidStudentClassNote
>;

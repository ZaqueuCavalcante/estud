namespace Estud.Back.Features.Teachers.CreateClassActivityWorkEntry;

[ApiController, Authorize(Policies.CreateClassActivityWorkEntry)]
public class CreateClassActivityWorkEntryController(CreateClassActivityWorkEntryService service) : ControllerBase
{
    /// <summary>
    /// Comentar, dar nota ou mudar o status de uma entrega
    /// </summary>
    /// <remarks>
    /// Registra na linha do tempo da entrega de um aluno o comentário, a nova nota e o novo status
    /// informados, em itens separados. Apenas o professor da turma pode fazer isso.
    /// </remarks>
    [HttpPost("teachers/activities/{activityId}/works/{workId}/entries")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int activityId, [FromRoute] int workId, [FromBody] CreateClassActivityWorkEntryIn data)
    {
        var result = await service.Create(activityId, workId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateClassActivityWorkEntryIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassActivityNotFound,
    TeacherNotAssignedToClass,
    ClassActivityWorkNotFound,
    InvalidClassActivityWorkEntry,
    InvalidClassActivityWorkContent,
    InvalidStudentClassNote
>;

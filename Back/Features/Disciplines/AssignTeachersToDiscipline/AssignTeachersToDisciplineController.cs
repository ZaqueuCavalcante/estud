namespace Estud.Back.Features.Disciplines.AssignTeachersToDiscipline;

[ApiController, Authorize(Policies.AssignTeachersToDiscipline)]
public class AssignTeachersToDisciplineController(AssignTeachersToDisciplineService service) : ControllerBase
{
    /// <summary>
    /// Vincular professores à disciplina
    /// </summary>
    /// <remarks>
    /// Define os professores aptos a lecionar a disciplina. Substitui a lista atual.
    /// </remarks>
    [HttpPut("disciplines/{disciplineId}/assign-teachers")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Assign([FromRoute] int disciplineId, [FromBody] AssignTeachersToDisciplineIn data)
    {
        var result = await service.Assign(disciplineId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<AssignTeachersToDisciplineIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    DisciplineNotFound,
    InvalidTeachersList
>;

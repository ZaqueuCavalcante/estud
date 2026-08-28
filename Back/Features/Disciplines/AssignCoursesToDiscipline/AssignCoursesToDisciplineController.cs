namespace Estud.Back.Features.Disciplines.AssignCoursesToDiscipline;

[ApiController, Authorize(Policies.AssignCoursesToDiscipline)]
public class AssignCoursesToDisciplineController(AssignCoursesToDisciplineService service) : ControllerBase
{
    /// <summary>
    /// Vincular cursos à disciplina
    /// </summary>
    /// <remarks>
    /// Define os cursos vinculados à disciplina. Substitui a lista atual.
    /// </remarks>
    [HttpPut("disciplines/{disciplineId}/assign-courses")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Assign([FromRoute] int disciplineId, [FromBody] AssignCoursesToDisciplineIn data)
    {
        var result = await service.Assign(disciplineId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<AssignCoursesToDisciplineIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    DisciplineNotFound,
    InvalidCoursesList
>;

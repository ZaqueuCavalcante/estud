namespace Estud.Back.Features.Disciplines.GetDiscipline;

[ApiController, Authorize(Policies.GetDiscipline)]
public class GetDisciplineController(GetDisciplineService service) : ControllerBase
{
    /// <summary>
    /// Disciplina
    /// </summary>
    /// <remarks>
    /// Retorna os dados de uma disciplina, incluindo os cursos vinculados.
    /// </remarks>
    [HttpGet("disciplines/{disciplineId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int disciplineId)
    {
        var result = await service.Get(disciplineId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetDisciplineOut>;
internal class ErrorsExamples : ErrorExamplesProvider<DisciplineNotFound>;

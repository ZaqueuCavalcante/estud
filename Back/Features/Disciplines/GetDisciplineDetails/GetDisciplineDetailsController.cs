namespace Estud.Back.Features.Disciplines.GetDisciplineDetails;

[ApiController, Authorize(Policies.GetDisciplineDetails)]
public class GetDisciplineDetailsController(GetDisciplineDetailsService service) : ControllerBase
{
    /// <summary>
    /// Buscar detalhes da disciplina
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes de uma disciplina da instituição do usuário logado, incluindo cursos, professores aptos e turmas.
    /// </remarks>
    [HttpGet("disciplines/{disciplineId}/details")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int disciplineId)
    {
        var result = await service.Get(disciplineId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetDisciplineDetailsOut>;
internal class ResponseExamples : ExamplesProvider<GetDisciplineDetailsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<DisciplineNotFound>;

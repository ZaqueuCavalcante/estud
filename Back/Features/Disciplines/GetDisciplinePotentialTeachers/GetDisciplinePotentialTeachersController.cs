namespace Estud.Back.Features.Disciplines.GetDisciplinePotentialTeachers;

[ApiController, Authorize(Policies.GetDisciplinePotentialTeachers)]
public class GetDisciplinePotentialTeachersController(GetDisciplinePotentialTeachersService service) : ControllerBase
{
    /// <summary>
    /// Professores disponíveis para vincular à disciplina
    /// </summary>
    /// <remarks>
    /// Retorna os professores ainda não aptos a lecionar a disciplina, com suporte a pesquisa por nome.
    /// </remarks>
    [HttpGet("disciplines/{disciplineId}/potential-teachers")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get(int disciplineId, [FromQuery] string? name)
    {
        var result = await service.Get(disciplineId, name);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetDisciplinePotentialTeachersOut>;
internal class ErrorsExamples : ErrorExamplesProvider<DisciplineNotFound>;

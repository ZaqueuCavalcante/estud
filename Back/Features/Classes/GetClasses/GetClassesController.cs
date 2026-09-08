namespace Estud.Back.Features.Classes.GetClasses;

[ApiController, Authorize(Policies.GetClasses)]
public class GetClassesController(GetClassesService service) : ControllerBase
{
    /// <summary>
    /// Turmas
    /// </summary>
    /// <remarks>
    /// Retorna a lista paginada de turmas da instituição do usuário logado,
    /// podendo filtrar por nome ou código da disciplina, por período e por status.
    /// </remarks>
    [HttpGet("classes")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get([FromQuery] GetClassesIn query)
    {
        var classes = await service.Get(query);
        return Ok(classes);
    }
}

internal class RequestExamples : ExamplesProvider<GetClassesIn>;
internal class ResponseExamples : ExamplesProvider<GetClassesOut>;

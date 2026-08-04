namespace Estud.Back.Features.Teachers.GetTeacherPotentialCampi;

[ApiController, Authorize(Policies.GetTeacherPotentialCampi)]
public class GetTeacherPotentialCampiController(GetTeacherPotentialCampiService service) : ControllerBase
{
    /// <summary>
    /// Campus disponíveis para vincular ao professor
    /// </summary>
    /// <remarks>
    /// Retorna os campus ainda não vinculados ao professor, com suporte a pesquisa por nome.
    /// </remarks>
    [HttpGet("teachers/{teacherId}/potential-campi")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int teacherId, [FromQuery] string? name)
    {
        var result = await service.Get(teacherId, name);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetTeacherPotentialCampiOut>;
internal class ErrorsExamples : ErrorExamplesProvider<TeacherNotFound>;

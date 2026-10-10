namespace Estud.Back.Features.Insights.GetLowestGradeClasses;

[ApiController, Authorize(Policies.GetLowestGradeClasses)]
public class GetLowestGradeClassesController(GetLowestGradeClassesService service) : ControllerBase
{
    /// <summary>
    /// Turmas com menor nota média
    /// </summary>
    /// <remarks>
    /// Retorna as turmas do período acadêmico informado com as menores notas médias,
    /// considerando apenas as turmas que já têm alguma nota lançada.
    /// </remarks>
    [HttpGet("insights/classes/lowest-grade")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetLowestGradeClassesIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetLowestGradeClassesIn>;
internal class ResponseExamples : ExamplesProvider<GetLowestGradeClassesOut>;
internal class ErrorsExamples : ErrorExamplesProvider<AcademicPeriodNotFound>;

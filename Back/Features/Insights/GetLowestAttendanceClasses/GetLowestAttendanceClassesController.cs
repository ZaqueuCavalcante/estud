namespace Estud.Back.Features.Insights.GetLowestAttendanceClasses;

[ApiController, Authorize(Policies.GetLowestAttendanceClasses)]
public class GetLowestAttendanceClassesController(GetLowestAttendanceClassesService service) : ControllerBase
{
    /// <summary>
    /// Turmas com menor frequência
    /// </summary>
    /// <remarks>
    /// Retorna as turmas do período acadêmico informado com as menores frequências médias,
    /// considerando apenas as turmas que já têm frequência lançada.
    /// </remarks>
    [HttpGet("insights/classes/lowest-attendance")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetLowestAttendanceClassesIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetLowestAttendanceClassesIn>;
internal class ResponseExamples : ExamplesProvider<GetLowestAttendanceClassesOut>;
internal class ErrorsExamples : ErrorExamplesProvider<AcademicPeriodNotFound>;

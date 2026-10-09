namespace Estud.Back.Features.Insights.GetPendingAttendance;

[ApiController, Authorize(Policies.GetPendingAttendance)]
public class GetPendingAttendanceController(GetPendingAttendanceService service) : ControllerBase
{
    /// <summary>
    /// Chamadas pendentes
    /// </summary>
    /// <remarks>
    /// Retorna o percentual de aulas já ocorridas no período acadêmico informado que tiveram a chamada lançada,
    /// junto com as turmas que mais acumulam aulas com chamada pendente.
    /// </remarks>
    [HttpGet("insights/classes/pending-attendance")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetPendingAttendanceIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetPendingAttendanceIn>;
internal class ResponseExamples : ExamplesProvider<GetPendingAttendanceOut>;
internal class ErrorsExamples : ErrorExamplesProvider<AcademicPeriodNotFound>;

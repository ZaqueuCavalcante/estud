namespace Estud.Back.Features.Insights.GetAttendance;

[ApiController, Authorize(Policies.GetAttendance)]
public class GetAttendanceController(GetAttendanceService service) : ControllerBase
{
    /// <summary>
    /// Frequência média das turmas
    /// </summary>
    /// <remarks>
    /// Retorna a frequência média diária das turmas do período acadêmico informado, considerando
    /// apenas os dias com frequência lançada, junto com a frequência média do período inteiro.
    /// </remarks>
    [HttpGet("insights/attendance")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetAttendanceIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetAttendanceIn>;
internal class ResponseExamples : ExamplesProvider<GetAttendanceOut>;
internal class ErrorsExamples : ErrorExamplesProvider<AcademicPeriodNotFound>;

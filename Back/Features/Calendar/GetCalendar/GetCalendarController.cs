namespace Estud.Back.Features.Calendar.GetCalendar;

[ApiController, Authorize(Policies.GetCalendar)]
public class GetCalendarController(GetCalendarService service) : ControllerBase
{
    /// <summary>
    /// Calendário acadêmico
    /// </summary>
    /// <remarks>
    /// Retorna todos os dias do ano informado, com o tipo de cada dia: dia letivo, férias, recesso ou feriado.
    /// Sem campus, o calendário é o da instituição. Com campus, os overrides daquele campus entram por cima.
    /// A precedência é campus, instituição e feriado nacional, e cada dia informa de qual desses níveis veio.
    /// </remarks>
    [HttpGet("calendar")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromQuery] GetCalendarIn data)
    {
        var result = await service.Get(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetCalendarIn>;
internal class ResponseExamples : ExamplesProvider<GetCalendarOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CampusNotFound>;

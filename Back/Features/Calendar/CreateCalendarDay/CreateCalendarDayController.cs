namespace Estud.Back.Features.Calendar.CreateCalendarDay;

[ApiController, Authorize(Policies.CreateCalendarDay)]
public class CreateCalendarDayController(CreateCalendarDayService service) : ControllerBase
{
    /// <summary>
    /// Customizar dias do calendário
    /// </summary>
    /// <remarks>
    /// Customiza um dia (ou um intervalo, quando há data final) do calendário acadêmico.
    /// </remarks>
    [HttpPost("calendar/days")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromBody] CreateCalendarDayIn data)
    {
        var result = await service.Create(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateCalendarDayIn>;
internal class ResponseExamples : ExamplesProvider<CreateCalendarDayOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidCalendarDayDate,
    InvalidCalendarDayRange,
    InvalidCalendarDayType,
    InvalidCalendarDayDescription,
    CampusNotFound,
    CalendarDayAlreadyExists
>;

namespace Estud.Back.Features.Calendar.DeleteCalendarDay;

[ApiController, Authorize(Policies.DeleteCalendarDay)]
public class DeleteCalendarDayController(DeleteCalendarDayService service) : ControllerBase
{
    /// <summary>
    /// Remover customização de dia do calendário
    /// </summary>
    /// <remarks>
    /// Remove o override e faz o dia voltar a herdar o tipo do nível acima: um dia de campus volta ao que a
    /// instituição define, e um dia de instituição volta ao feriado nacional, fim de semana ou dia letivo.
    /// </remarks>
    [HttpDelete("calendar/days/{dayId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Delete([FromRoute] int dayId)
    {
        var result = await service.Delete(dayId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CalendarDayNotFound>;

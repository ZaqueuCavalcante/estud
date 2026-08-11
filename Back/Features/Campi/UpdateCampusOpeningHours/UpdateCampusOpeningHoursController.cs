namespace Estud.Back.Features.Campi.UpdateCampusOpeningHours;

[ApiController, Authorize(Policies.UpdateCampusOpeningHours)]
public class UpdateCampusOpeningHoursController(UpdateCampusOpeningHoursService service) : ControllerBase
{
    /// <summary>
    /// Definir horários de funcionamento do campus
    /// </summary>
    /// <remarks>
    /// Define a semana de funcionamento do campus. Substitui a configuração atual (replace-all).
    /// Dia omitido, ou com a lista de janelas vazia, significa que o campus não abre nele.
    /// </remarks>
    [HttpPut("campi/{campusId}/opening-hours")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int campusId, [FromBody] UpdateCampusOpeningHoursIn data)
    {
        var result = await service.Update(campusId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateCampusOpeningHoursIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    CampusNotFound,
    InvalidOpeningHour,
    InvalidOpeningHoursList,
    OverlappingOpeningHours,
    OpeningHourOutsideShift,
    MultipleOpeningHoursInShift
>;

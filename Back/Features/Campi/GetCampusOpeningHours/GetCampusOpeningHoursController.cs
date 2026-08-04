namespace Estud.Back.Features.Campi.GetCampusOpeningHours;

[ApiController, Authorize(Policies.GetCampusOpeningHours)]
public class GetCampusOpeningHoursController(GetCampusOpeningHoursService service) : ControllerBase
{
    /// <summary>
    /// Horários de funcionamento do campus
    /// </summary>
    /// <remarks>
    /// Retorna a semana de funcionamento do campus, com um item por dia da semana.
    /// Um dia sem janelas significa que o campus não abre naquele dia.
    /// </remarks>
    [HttpGet("campi/{campusId}/opening-hours")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int campusId)
    {
        var result = await service.Get(campusId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCampusOpeningHoursOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CampusNotFound>;

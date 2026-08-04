namespace Estud.Back.Features.Campi.GetCampusOccupancy;

[ApiController, Authorize(Policies.GetCampusOccupancy)]
public class GetCampusOccupancyController(GetCampusOccupancyService service) : ControllerBase
{
    /// <summary>
    /// Ocupação do campus
    /// </summary>
    /// <remarks>
    /// Retorna o mapa de ocupação das salas de um campus, agregado por dia
    /// da semana e turno, com o detalhamento sala a sala de cada célula.
    /// </remarks>
    [HttpGet("campi/{campusId}/occupancy")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int campusId)
    {
        var result = await service.Get(campusId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCampusOccupancyOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CampusNotFound>;

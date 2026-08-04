namespace Estud.Back.Features.Campi.UpdateCampus;

[ApiController, Authorize(Policies.UpdateCampus)]
public class UpdateCampusController(UpdateCampusService service) : ControllerBase
{
    /// <summary>
    /// Editar campus
    /// </summary>
    /// <remarks>
    /// Edita os dados do campus informado.
    /// </remarks>
    [HttpPut("campi/{campusId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int campusId, [FromBody] UpdateCampusIn data)
    {
        var result = await service.Update(campusId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateCampusIn>;
internal class ResponseExamples : ExamplesProvider<UpdateCampusOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidCampusName,
    InvalidBrazilState,
    InvalidCampusCity,
    CampusNotFound
>;

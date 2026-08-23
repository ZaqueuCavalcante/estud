namespace Estud.Back.Features.CourseOfferings.GetCourseOfferingDetails;

[ApiController, Authorize(Policies.GetCourseOfferingDetails)]
public class GetCourseOfferingDetailsController(GetCourseOfferingDetailsService service) : ControllerBase
{
    /// <summary>
    /// Detalhes da oferta de curso
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes de uma oferta de curso, incluindo o campus, o curso, a grade curricular,
    /// o período acadêmico, o turno e os alunos matriculados.
    /// </remarks>
    [HttpGet("course-offerings/{courseOfferingId}/details")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int courseOfferingId)
    {
        var result = await service.Get(courseOfferingId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCourseOfferingDetailsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CourseOfferingNotFound>;

namespace Estud.Back.Features.Courses.GetCourseDetails;

[ApiController, Authorize(Policies.GetCourseDetails)]
public class GetCourseDetailsController(GetCourseDetailsService service) : ControllerBase
{
    /// <summary>
    /// Detalhes do curso
    /// </summary>
    /// <remarks>
    /// Retorna os detalhes de um curso da instituição do usuário logado, incluindo as disciplinas vinculadas,
    /// as grades curriculares e as ofertas do curso.
    /// </remarks>
    [HttpGet("courses/{courseId}/details")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int courseId)
    {
        var result = await service.Get(courseId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCourseDetailsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CourseNotFound>;

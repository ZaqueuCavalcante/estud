namespace Estud.Back.Features.Courses.GetCourse;

[ApiController, Authorize(Policies.GetCourse)]
public class GetCourseController(GetCourseService service) : ControllerBase
{
    /// <summary>
    /// Curso
    /// </summary>
    /// <remarks>
    /// Retorna os dados de um curso, incluindo as disciplinas vinculadas.
    /// </remarks>
    [HttpGet("courses/{courseId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int courseId)
    {
        var result = await service.Get(courseId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetCourseOut>;
internal class ErrorsExamples : ErrorExamplesProvider<CourseNotFound>;

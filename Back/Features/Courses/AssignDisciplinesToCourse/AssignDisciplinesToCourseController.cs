namespace Estud.Back.Features.Courses.AssignDisciplinesToCourse;

[ApiController, Authorize(Policies.AssignDisciplinesToCourse)]
public class AssignDisciplinesToCourseController(AssignDisciplinesToCourseService service) : ControllerBase
{
    /// <summary>
    /// Vincular disciplinas ao curso
    /// </summary>
    /// <remarks>
    /// Define as disciplinas vinculadas ao curso. Substitui a lista atual.
    /// </remarks>
    [HttpPut("courses/{courseId}/assign-disciplines")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Assign([FromRoute] int courseId, [FromBody] AssignDisciplinesToCourseIn data)
    {
        var result = await service.Assign(courseId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<AssignDisciplinesToCourseIn>;
internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    CourseNotFound,
    InvalidDisciplinesList
>;

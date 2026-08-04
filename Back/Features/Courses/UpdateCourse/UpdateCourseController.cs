namespace Estud.Back.Features.Courses.UpdateCourse;

[ApiController, Authorize(Policies.UpdateCourse)]
public class UpdateCourseController(UpdateCourseService service) : ControllerBase
{
    /// <summary>
    /// Editar curso
    /// </summary>
    /// <remarks>
    /// Edita o nome e o tipo do curso informado.
    /// </remarks>
    [HttpPut("courses/{courseId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int courseId, [FromBody] UpdateCourseIn data)
    {
        var result = await service.Update(courseId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateCourseIn>;
internal class ResponseExamples : ExamplesProvider<UpdateCourseOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidCourseName,
    InvalidCourseType,
    CourseNotFound
>;

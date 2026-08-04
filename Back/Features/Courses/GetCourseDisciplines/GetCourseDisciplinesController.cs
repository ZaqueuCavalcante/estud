namespace Estud.Back.Features.Courses.GetCourseDisciplines;

[ApiController, Authorize(Policies.GetCourseDisciplines)]
public class GetCourseDisciplinesController(GetCourseDisciplinesService service) : ControllerBase
{
    /// <summary>
    /// Disciplinas do curso
    /// </summary>
    /// <remarks>
    /// Retorna todas as disciplinas do curso informado.
    /// </remarks>
    [HttpGet("courses/{courseId}/disciplines")]
    public async Task<IActionResult> Get([FromRoute] int courseId)
    {
        var disciplines = await service.Get(courseId);
        return Ok(disciplines);
    }
}

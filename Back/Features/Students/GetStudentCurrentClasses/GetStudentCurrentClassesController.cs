namespace Estud.Back.Features.Students.GetStudentCurrentClasses;

[ApiController, Authorize(Policies.GetStudentCurrentClasses)]
public class GetStudentCurrentClassesController(GetStudentCurrentClassesService service) : ControllerBase
{
    /// <summary>
    /// Turmas atuais
    /// </summary>
    /// <remarks>
    /// Retorna as turmas que o aluno está cursando atualmente.
    /// </remarks>
    [HttpGet("students/current-classes")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get()
    {
        var data = await service.Get();
        return Ok(data);
    }
}

internal class ResponseExamples : ExamplesProvider<GetStudentCurrentClassesOut>;

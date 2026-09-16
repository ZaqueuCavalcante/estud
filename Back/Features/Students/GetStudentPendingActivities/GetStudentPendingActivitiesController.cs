namespace Estud.Back.Features.Students.GetStudentPendingActivities;

[ApiController, Authorize(Policies.GetStudentPendingActivities)]
public class GetStudentPendingActivitiesController(GetStudentPendingActivitiesService service) : ControllerBase
{
    /// <summary>
    /// Atividades pendentes
    /// </summary>
    /// <remarks>
    /// Retorna o total de atividades que o aluno logado ainda não entregou,
    /// considerando as turmas iniciadas em que ele está matriculado.
    /// </remarks>
    [HttpGet("students/pending-activities")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Get()
    {
        var data = await service.Get();
        return Ok(data);
    }
}

internal class ResponseExamples : ExamplesProvider<GetStudentPendingActivitiesOut>;

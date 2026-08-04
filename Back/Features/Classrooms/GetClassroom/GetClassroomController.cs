namespace Estud.Back.Features.Classrooms.GetClassroom;

[ApiController, Authorize(Policies.GetClassroom)]
public class GetClassroomController(GetClassroomService service) : ControllerBase
{
    /// <summary>
    /// Buscar sala de aula
    /// </summary>
    /// <remarks>
    /// Retorna os dados de uma sala de aula da instituição do usuário logado, incluindo a sua agenda (turmas alocadas, dias e horários).
    /// </remarks>
    [HttpGet("classrooms/{classroomId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classroomId)
    {
        var result = await service.Get(classroomId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetClassroomOut>;
internal class ResponseExamples : ExamplesProvider<GetClassroomOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassroomNotFound>;

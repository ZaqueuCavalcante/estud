namespace Estud.Back.Features.Classes.GetClassSchedules;

[ApiController, Authorize(Policies.GetClassSchedules)]
public class GetClassSchedulesController(GetClassSchedulesService service) : ControllerBase
{
    /// <summary>
    /// Buscar horários da turma
    /// </summary>
    /// <remarks>
    /// Retorna os horários semanais da turma e, na mesma lista, os horários que os professores
    /// da turma já ocupam em outras turmas não finalizadas. Esses últimos vêm com `FromOtherClass`
    /// verdadeiro e servem para mostrar quais horários não podem ser usados pela turma.
    /// </remarks>
    [HttpGet("classes/{classId}/schedules")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId)
    {
        var result = await service.Get(classId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetClassSchedulesOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound>;

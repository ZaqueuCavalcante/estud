namespace Estud.Back.Features.Teachers.GetTeacherClassLessons;

[ApiController, Authorize(Policies.GetTeacherClassLessons)]
public class GetTeacherClassLessonsController(GetTeacherClassLessonsService service) : ControllerBase
{
    /// <summary>
    /// Aulas da turma
    /// </summary>
    /// <remarks>
    /// Retorna as aulas de uma turma lecionada pelo professor logado,
    /// com os alunos marcados como presentes na chamada de cada aula.
    /// Aceita um termo de busca opcional, que filtra as aulas pelo conteúdo do plano de aula.
    /// </remarks>
    [HttpGet("teachers/classes/{classId}/lessons")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromQuery] GetTeacherClassLessonsIn query)
    {
        var result = await service.Get(classId, query);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetTeacherClassLessonsIn>;
internal class ResponseExamples : ExamplesProvider<GetTeacherClassLessonsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<InvalidClassLessonSearch, ClassNotFound, TeacherNotAssignedToClass>;

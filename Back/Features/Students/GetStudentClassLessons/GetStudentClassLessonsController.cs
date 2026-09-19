namespace Estud.Back.Features.Students.GetStudentClassLessons;

[ApiController, Authorize(Policies.GetStudentClassLessons)]
public class GetStudentClassLessonsController(GetStudentClassLessonsService service) : ControllerBase
{
    /// <summary>
    /// Aulas da turma do aluno
    /// </summary>
    /// <remarks>
    /// Retorna as aulas de uma turma em que o aluno logado está matriculado.
    /// Aceita um termo de busca opcional, que filtra as aulas pelo conteúdo do plano de aula.
    /// </remarks>
    [HttpGet("students/classes/{classId}/lessons")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromQuery] GetStudentClassLessonsIn query)
    {
        var result = await service.Get(classId, query);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<GetStudentClassLessonsIn>;
internal class ResponseExamples : ExamplesProvider<GetStudentClassLessonsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<InvalidClassLessonSearch, ClassNotFound, StudentNotEnrolledInClass>;

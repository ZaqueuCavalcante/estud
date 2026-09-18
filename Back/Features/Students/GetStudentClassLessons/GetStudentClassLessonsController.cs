namespace Estud.Back.Features.Students.GetStudentClassLessons;

[ApiController, Authorize(Policies.GetStudentClassLessons)]
public class GetStudentClassLessonsController(GetStudentClassLessonsService service) : ControllerBase
{
    /// <summary>
    /// Aulas da turma do aluno
    /// </summary>
    /// <remarks>
    /// Retorna as aulas de uma turma em que o aluno logado está matriculado.
    /// </remarks>
    [HttpGet("students/classes/{classId}/lessons")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId)
    {
        var result = await service.Get(classId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetStudentClassLessonsOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound, StudentNotEnrolledInClass>;

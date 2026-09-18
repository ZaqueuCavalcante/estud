namespace Estud.Back.Features.Students.GetStudentClassLesson;

[ApiController, Authorize(Policies.GetStudentClassLesson)]
public class GetStudentClassLessonController(GetStudentClassLessonService service) : ControllerBase
{
    /// <summary>
    /// Aula da turma do aluno
    /// </summary>
    /// <remarks>
    /// Retorna uma aula de uma turma em que o aluno logado está matriculado,
    /// com o planejamento que o professor gravou para ela.
    /// </remarks>
    [HttpGet("students/classes/{classId}/lessons/{lessonId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromRoute] int lessonId)
    {
        var result = await service.Get(classId, lessonId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetStudentClassLessonOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound, StudentNotEnrolledInClass, ClassLessonNotFound>;

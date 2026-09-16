namespace Estud.Back.Features.Teachers.GetTeacherClassLesson;

[ApiController, Authorize(Policies.GetTeacherClassLesson)]
public class GetTeacherClassLessonController(GetTeacherClassLessonService service) : ControllerBase
{
    /// <summary>
    /// Aula da turma
    /// </summary>
    /// <remarks>
    /// Retorna uma aula de uma turma lecionada pelo professor logado,
    /// com o planejamento e a presença de cada aluno matriculado.
    /// </remarks>
    [HttpGet("teachers/classes/{classId}/lessons/{lessonId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId, [FromRoute] int lessonId)
    {
        var result = await service.Get(classId, lessonId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetTeacherClassLessonOut>;
internal class ErrorsExamples : ErrorExamplesProvider<ClassNotFound, TeacherNotAssignedToClass, ClassLessonNotFound>;

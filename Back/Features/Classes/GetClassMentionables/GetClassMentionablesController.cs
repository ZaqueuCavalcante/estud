namespace Estud.Back.Features.Classes.GetClassMentionables;

[ApiController, Authorize(Policies.GetClassMentionables)]
public class GetClassMentionablesController(GetClassMentionablesService service) : ControllerBase
{
    /// <summary>
    /// Buscar pessoas mencionáveis da turma
    /// </summary>
    /// <remarks>
    /// Retorna os alunos da turma que podem ser mencionados num editor de texto rico.
    /// Disponível para professores vinculados à turma e para alunos matriculados nela.
    /// </remarks>
    [HttpGet("classes/{classId}/mentionables")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Get([FromRoute] int classId)
    {
        var result = await service.Get(classId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<GetClassMentionablesOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ClassNotFound,
    TeacherNotAssignedToClass,
    StudentNotEnrolledInClass
>;

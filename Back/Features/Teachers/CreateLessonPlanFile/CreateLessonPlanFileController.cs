namespace Estud.Back.Features.Teachers.CreateLessonPlanFile;

[ApiController, Authorize(Policies.CreateLessonPlanFile)]
public class CreateLessonPlanFileController(CreateLessonPlanFileService service) : ControllerBase
{
    /// <summary>
    /// Enviar arquivo do planejamento de aula
    /// </summary>
    /// <remarks>
    /// Gera uma URL pré-assinada para o professor enviar uma imagem ou PDF direto para o storage,
    /// junto com a URL pública que deve ser referenciada no markdown do planejamento.
    /// A URL de envio expira em 10 minutos e exige o mesmo Content-Type e tamanho informados aqui.
    /// Apenas o professor da turma pode enviar arquivos para a aula.
    /// </remarks>
    [HttpPost("teachers/lessons/{lessonId}/plan/files")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int lessonId, [FromBody] CreateLessonPlanFileIn data)
    {
        var result = await service.Create(lessonId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateLessonPlanFileIn>;
internal class ResponseExamples : ExamplesProvider<CreateLessonPlanFileOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidLessonPlanFileContentType,
    InvalidLessonPlanFileSize,
    ClassLessonNotFound,
    TeacherNotAssignedToClass
>;

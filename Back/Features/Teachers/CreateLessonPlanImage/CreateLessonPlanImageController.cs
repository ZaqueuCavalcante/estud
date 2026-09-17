namespace Estud.Back.Features.Teachers.CreateLessonPlanImage;

[ApiController, Authorize(Policies.CreateLessonPlanImage)]
public class CreateLessonPlanImageController(CreateLessonPlanImageService service) : ControllerBase
{
    /// <summary>
    /// Enviar imagem do planejamento de aula
    /// </summary>
    /// <remarks>
    /// Gera uma URL pré-assinada para o professor enviar uma imagem direto para o storage,
    /// junto com a URL pública que deve ser referenciada no markdown do planejamento.
    /// A URL de envio expira em 10 minutos e exige o mesmo Content-Type informado aqui.
    /// Apenas o professor da turma pode enviar imagens para a aula.
    /// </remarks>
    [HttpPost("teachers/lessons/{lessonId}/plan/images")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int lessonId, [FromBody] CreateLessonPlanImageIn data)
    {
        var result = await service.Create(lessonId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateLessonPlanImageIn>;
internal class ResponseExamples : ExamplesProvider<CreateLessonPlanImageOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidLessonPlanImageContentType,
    InvalidLessonPlanImageSize,
    ClassLessonNotFound,
    TeacherNotAssignedToClass
>;

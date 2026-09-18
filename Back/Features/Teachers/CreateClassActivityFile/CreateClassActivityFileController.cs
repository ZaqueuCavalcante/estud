namespace Estud.Back.Features.Teachers.CreateClassActivityFile;

[ApiController, Authorize(Policies.CreateClassActivityFile)]
public class CreateClassActivityFileController(CreateClassActivityFileService service) : ControllerBase
{
    /// <summary>
    /// Enviar arquivo da atividade
    /// </summary>
    /// <remarks>
    /// Gera uma URL pré-assinada para o professor enviar uma imagem ou PDF direto para o storage,
    /// junto com a URL pública que deve ser referenciada no markdown da descrição da atividade.
    /// A URL de envio expira em 10 minutos e exige o mesmo Content-Type e tamanho informados aqui.
    /// Apenas o professor da turma pode enviar arquivos para as atividades dela.
    /// </remarks>
    [HttpPost("teachers/classes/{classId}/activities/files")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int classId, [FromBody] CreateClassActivityFileIn data)
    {
        var result = await service.Create(classId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateClassActivityFileIn>;
internal class ResponseExamples : ExamplesProvider<CreateClassActivityFileOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidClassActivityFileContentType,
    InvalidClassActivityFileSize,
    ClassNotFound,
    TeacherNotAssignedToClass
>;

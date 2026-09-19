namespace Estud.Back.Features.Students.CreateClassActivityWorkFile;

[ApiController, Authorize(Policies.CreateClassActivityWorkFile)]
public class CreateClassActivityWorkFileController(CreateClassActivityWorkFileService service) : ControllerBase
{
    /// <summary>
    /// Enviar arquivo da entrega
    /// </summary>
    /// <remarks>
    /// Gera uma URL pré-assinada para o aluno enviar uma imagem ou PDF direto para o storage,
    /// junto com a URL pública que deve ser referenciada no markdown da entrega da atividade.
    /// A URL de envio expira em 10 minutos e exige o mesmo Content-Type e tamanho informados aqui.
    /// Apenas alunos da turma podem enviar arquivos, e somente para atividades que aceitam entregas.
    /// </remarks>
    [HttpPost("students/activities/{classActivityId}/works/files")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int classActivityId, [FromBody] CreateClassActivityWorkFileIn data)
    {
        var result = await service.Create(classActivityId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateClassActivityWorkFileIn>;
internal class ResponseExamples : ExamplesProvider<CreateClassActivityWorkFileOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidClassActivityWorkFileContentType,
    InvalidClassActivityWorkFileSize,
    ClassActivityNotFound,
    ClassActivityDoesNotAcceptWorks,
    StudentNotEnrolledInClass
>;

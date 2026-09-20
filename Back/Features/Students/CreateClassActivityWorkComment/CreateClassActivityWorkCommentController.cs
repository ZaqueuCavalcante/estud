namespace Estud.Back.Features.Students.CreateClassActivityWorkComment;

[ApiController, Authorize(Policies.CreateClassActivityWorkComment)]
public class CreateClassActivityWorkCommentController(CreateClassActivityWorkCommentService service) : ControllerBase
{
    /// <summary>
    /// Comentar em entrega
    /// </summary>
    /// <remarks>
    /// Adiciona um comentário do aluno logado na linha do tempo da sua entrega,
    /// em markdown (texto, imagens e PDFs). Só é possível até o professor finalizar a entrega.
    /// </remarks>
    [HttpPost("students/activities/{classActivityId}/works/comments")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromRoute] int classActivityId, [FromBody] CreateClassActivityWorkCommentIn data)
    {
        var result = await service.Create(classActivityId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateClassActivityWorkCommentIn>;
internal class ResponseExamples : ExamplesProvider<CreateClassActivityWorkCommentOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidClassActivityWorkContent,
    ClassActivityNotFound,
    ClassActivityDoesNotAcceptWorks,
    StudentNotEnrolledInClass,
    ClassActivityWorkNotFound,
    ClassActivityWorkAlreadyFinalized
>;

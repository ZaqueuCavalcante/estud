namespace Estud.Back.Features.Parents.RevokeParentStudentLink;

[ApiController, Authorize(Policies.RevokeParentStudentLink)]
public class RevokeParentStudentLinkController(RevokeParentStudentLinkService service) : ControllerBase
{
    /// <summary>
    /// Revogar vínculo entre responsável e aluno
    /// </summary>
    /// <remarks>
    /// Revoga o vínculo entre um responsável e um aluno da instituição, removendo o acesso do responsável aos dados do aluno.
    /// </remarks>
    [HttpPut("parents/{parentId}/students/{studentId}/revoke")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Revoke([FromRoute] int parentId, [FromRoute] int studentId)
    {
        var result = await service.Revoke(parentId, studentId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ParentStudentLinkNotFound,
    ParentStudentLinkAlreadyRevoked
>;

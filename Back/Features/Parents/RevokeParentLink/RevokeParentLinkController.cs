namespace Estud.Back.Features.Parents.RevokeParentLink;

[ApiController, Authorize(Policies.RevokeParentLink)]
public class RevokeParentLinkController(RevokeParentLinkService service) : ControllerBase
{
    /// <summary>
    /// Revogar acesso do responsável
    /// </summary>
    /// <remarks>
    /// Revoga o acesso de um responsável aos dados do aluno logado. Disponível apenas para alunos maiores de 18 anos.
    /// </remarks>
    [HttpPut("parents/{parentId}/revoke")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Revoke([FromRoute] int parentId)
    {
        var result = await service.Revoke(parentId);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    ParentStudentLinkNotFound,
    ParentStudentLinkAlreadyRevoked,
    StudentMustBeAdult
>;

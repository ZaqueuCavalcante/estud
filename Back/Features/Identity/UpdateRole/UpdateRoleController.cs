namespace Estud.Back.Features.Identity.UpdateRole;

[ApiController, Authorize(Policies.UpdateRole)]
public class UpdateRoleController(UpdateRoleService service) : ControllerBase
{
    /// <summary>
    /// Editar perfil de acesso
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de um perfil de acesso da instituição do usuário logado.
    /// </remarks>
    [HttpPut("identity/roles/{roleId}")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromRoute] int roleId, [FromBody] UpdateRoleIn data)
    {
        var result = await service.Update(roleId, data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateRoleIn>;
internal class ResponseExamples : ExamplesProvider<UpdateRoleOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    RoleNotFound,
    InvalidRoleName,
    InvalidRoleDescription,
    InvalidPermissionsList,
    InvalidPermissionsForUserType,
    RoleNameAlreadyExists,
    InvalidRolePermissions
>;

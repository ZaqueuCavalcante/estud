namespace Estud.Back.Features.Users.RemoveProfilePhoto;

[ApiController, Authorize(Policies.RemoveProfilePhoto)]
public class RemoveProfilePhotoController(RemoveProfilePhotoService service) : ControllerBase
{
    /// <summary>
    /// Remover foto de perfil
    /// </summary>
    /// <remarks>
    /// Remove a foto de perfil do usuário logado, apagando o arquivo do storage.
    /// Se o usuário não tiver foto, nada é feito.
    /// </remarks>
    [HttpDelete("users/account/profile-photo")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    public async Task<IActionResult> Remove()
    {
        var result = await service.Remove();
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class ResponseExamples : ExamplesProvider<SuccessOut>;

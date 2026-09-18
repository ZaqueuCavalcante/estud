namespace Estud.Back.Features.Users.UpdateProfilePhoto;

[ApiController, Authorize(Policies.UpdateProfilePhoto)]
public class UpdateProfilePhotoController(UpdateProfilePhotoService service) : ControllerBase
{
    /// <summary>
    /// Atualizar foto de perfil
    /// </summary>
    /// <remarks>
    /// Confirma a foto enviada via URL pré-assinada como a nova foto de perfil do usuário logado.
    /// A foto anterior, se existir, é apagada do storage.
    /// </remarks>
    [HttpPut("users/account/profile-photo")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Update([FromBody] UpdateProfilePhotoIn data)
    {
        var result = await service.Update(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<UpdateProfilePhotoIn>;
internal class ResponseExamples : ExamplesProvider<UpdateProfilePhotoOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidProfilePhotoPath,
    ProfilePhotoNotFound,
    InvalidProfilePhotoContentType,
    InvalidProfilePhotoSize
>;

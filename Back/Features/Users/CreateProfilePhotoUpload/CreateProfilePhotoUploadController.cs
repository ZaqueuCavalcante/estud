namespace Estud.Back.Features.Users.CreateProfilePhotoUpload;

[ApiController, Authorize(Policies.CreateProfilePhotoUpload)]
public class CreateProfilePhotoUploadController(CreateProfilePhotoUploadService service) : ControllerBase
{
    /// <summary>
    /// Enviar foto de perfil
    /// </summary>
    /// <remarks>
    /// Gera uma URL pré-assinada para o usuário enviar sua foto de perfil direto para o storage.
    /// A URL de envio expira em 10 minutos e exige o mesmo Content-Type e tamanho informados aqui.
    /// Depois do envio, a foto só passa a valer ao confirmar o path retornado em PUT users/account/profile-photo.
    /// </remarks>
    [HttpPost("users/account/profile-photo/upload")]
    [SwaggerResponseExample(200, typeof(ResponseExamples))]
    [SwaggerResponseExample(400, typeof(ErrorsExamples))]
    public async Task<IActionResult> Create([FromBody] CreateProfilePhotoUploadIn data)
    {
        var result = await service.Create(data);
        return result.Match<IActionResult>(Ok, BadRequest);
    }
}

internal class RequestExamples : ExamplesProvider<CreateProfilePhotoUploadIn>;
internal class ResponseExamples : ExamplesProvider<CreateProfilePhotoUploadOut>;
internal class ErrorsExamples : ErrorExamplesProvider<
    InvalidProfilePhotoContentType,
    InvalidProfilePhotoSize
>;

namespace Estud.Back.Features.Users.UpdateProfilePhoto;

public class UpdateProfilePhotoOut : IApiDto<UpdateProfilePhotoOut>
{
    /// <summary>
    /// URL pública da nova foto de perfil
    /// </summary>
    public string ProfilePhoto { get; set; }

    public static IEnumerable<(string, UpdateProfilePhotoOut)> GetExamples() =>
    [
        ("Exemplo", new UpdateProfilePhotoOut { ProfilePhoto = "https://cdn.estud.com.br/profile-photos/1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.jpg" }),
    ];
}

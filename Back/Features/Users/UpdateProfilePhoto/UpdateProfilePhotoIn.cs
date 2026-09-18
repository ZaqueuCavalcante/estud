namespace Estud.Back.Features.Users.UpdateProfilePhoto;

public class UpdateProfilePhotoIn : IApiDto<UpdateProfilePhotoIn>
{
    /// <summary>
    /// Caminho da foto retornado em POST users/account/profile-photo/upload
    /// </summary>
    public string? Path { get; set; }

    public static IEnumerable<(string, UpdateProfilePhotoIn)> GetExamples() =>
    [
        ("Exemplo", new UpdateProfilePhotoIn { Path = "1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.jpg" }),
    ];
}

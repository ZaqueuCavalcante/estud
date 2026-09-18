namespace Estud.Back.Features.Users.CreateProfilePhotoUpload;

public class CreateProfilePhotoUploadOut : IApiDto<CreateProfilePhotoUploadOut>
{
    /// <summary>
    /// URL pré-assinada para enviar a foto via PUT
    /// </summary>
    public string UploadUrl { get; set; }

    /// <summary>
    /// Caminho da foto no storage, para confirmar em PUT users/account/profile-photo
    /// </summary>
    public string Path { get; set; }

    public static IEnumerable<(string, CreateProfilePhotoUploadOut)> GetExamples() =>
    [
        ("Exemplo", new CreateProfilePhotoUploadOut
        {
            UploadUrl = "https://<account-id>.r2.cloudflarestorage.com/estud-files/profile-photos/1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.jpg?X-Amz-Signature=...",
            Path = "1/2/01K5B4Z8Q2M3N4P5R6S7T8V9W0.jpg",
        }),
    ];
}

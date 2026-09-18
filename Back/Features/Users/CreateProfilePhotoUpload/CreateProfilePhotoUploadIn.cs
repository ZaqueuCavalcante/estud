namespace Estud.Back.Features.Users.CreateProfilePhotoUpload;

public class CreateProfilePhotoUploadIn : IApiDto<CreateProfilePhotoUploadIn>
{
    /// <summary>
    /// Content-Type da foto (image/png, image/jpeg ou image/webp)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Tamanho da foto em bytes (máximo de 3 MB)
    /// </summary>
    public long SizeInBytes { get; set; }

    public static IEnumerable<(string, CreateProfilePhotoUploadIn)> GetExamples() =>
    [
        ("Exemplo", new CreateProfilePhotoUploadIn { ContentType = "image/jpeg", SizeInBytes = 845_000 }),
    ];
}

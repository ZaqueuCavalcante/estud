using Estud.Back.Storage;

namespace Estud.Back.Features.Users.CreateProfilePhotoUpload;

public class CreateProfilePhotoUploadService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    public const long MaxSizeInBytes = 3 * 1024 * 1024;
    private static readonly TimeSpan UploadUrlExpiration = TimeSpan.FromMinutes(10);

    public static readonly Dictionary<string, string> FileExtensions = new()
    {
        ["image/png"] = "png",
        ["image/jpeg"] = "jpg",
        ["image/webp"] = "webp",
    };

    private class Validator : AbstractValidator<CreateProfilePhotoUploadIn>
    {
        public Validator()
        {
            RuleFor(x => x.ContentType).Must(x => x != null && FileExtensions.ContainsKey(x)).WithError(InvalidProfilePhotoContentType.I);
            RuleFor(x => x.SizeInBytes).InclusiveBetween(1, MaxSizeInBytes).WithError(InvalidProfilePhotoSize.I);
        }
    }
    private static readonly Validator V = new();

    public async Task<OneOf<CreateProfilePhotoUploadOut, EstudError>> Create(CreateProfilePhotoUploadIn data)
    {
        if (V.Run(data, out var error)) return error;

        var path = $"{ctx.RequestUser.InstitutionId}/{ctx.RequestUser.Id}/{Ulid.NewUlid()}.{FileExtensions[data.ContentType]}";
        var url = await storage.CreatePreSignedUrlForUpload(StorageContainer.ProfilePhotos, path, data.ContentType, data.SizeInBytes, UploadUrlExpiration);

        return new CreateProfilePhotoUploadOut { UploadUrl = url, Path = path };
    }
}

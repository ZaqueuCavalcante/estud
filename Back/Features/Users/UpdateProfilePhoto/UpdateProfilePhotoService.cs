using Estud.Back.Storage;
using System.Text.RegularExpressions;
using Estud.Back.Features.Users.CreateProfilePhotoUpload;

namespace Estud.Back.Features.Users.UpdateProfilePhoto;

public partial class UpdateProfilePhotoService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    [GeneratedRegex(@"^[0-9A-Z]{26}\.(png|jpg|webp)$")]
    private static partial Regex FileName();

    public async Task<OneOf<UpdateProfilePhotoOut, EstudError>> Update(UpdateProfilePhotoIn data)
    {
        var prefix = $"{ctx.RequestUser.InstitutionId}/{ctx.RequestUser.Id}/";
        if (data.Path.IsEmpty() || !data.Path.StartsWith(prefix) || !FileName().IsMatch(data.Path[prefix.Length..]))
            return InvalidProfilePhotoPath.I;

        var container = StorageContainer.ProfilePhotos;

        var metadata = await storage.GetMetadata(container, data.Path);
        if (metadata == null) return ProfilePhotoNotFound.I;

        if (!CreateProfilePhotoUploadService.FileExtensions.ContainsKey(metadata.ContentType))
            return InvalidProfilePhotoContentType.I;

        if (metadata.SizeInBytes > CreateProfilePhotoUploadService.MaxSizeInBytes)
            return InvalidProfilePhotoSize.I;

        var user = await ctx.Users.FirstAsync(u => u.Id == ctx.RequestUser.Id);
        var oldPath = user.ProfilePhoto;

        user.ProfilePhoto = data.Path;
        await ctx.SaveChangesAsync();

        if (oldPath.HasValue() && oldPath != data.Path) await storage.Delete(container, oldPath);

        return new UpdateProfilePhotoOut { ProfilePhoto = storage.GetPublicUrl(container, data.Path) };
    }
}

using Estud.Back.Storage;

namespace Estud.Back.Features.Users.RemoveProfilePhoto;

public class RemoveProfilePhotoService(EstudDbContext ctx, IStorageService storage) : IEstudService
{
    public async Task<OneOf<EstudSuccess, EstudError>> Remove()
    {
        var user = await ctx.Users.FirstAsync(u => u.Id == ctx.RequestUser.Id);
        if (user.ProfilePhoto.IsEmpty()) return EstudSuccess.I;

        var path = user.ProfilePhoto;
        user.ProfilePhoto = null;
        await ctx.SaveChangesAsync();

        await storage.Delete(StorageContainer.ProfilePhotos, path);

        return EstudSuccess.I;
    }
}

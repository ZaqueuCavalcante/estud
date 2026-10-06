namespace Estud.Back.Extensions;

public static class StorageExtensions
{
    private static string? _publicBaseUrl;

    public static void ConfigurePublicBaseUrl(string publicBaseUrl) => _publicBaseUrl = publicBaseUrl;

    extension(StorageContainer container)
    {
        public string GetKey(string path) => $"{container.GetDescription()}/{path}";

        public string GetPublicUrl(string path)
        {
            if (!container.IsPublic) throw new InvalidOperationException($"{container} is a private container, use a pre-signed url!");
            if (_publicBaseUrl == null) throw new InvalidOperationException($"Storage public base url not configured, call {nameof(ConfigurePublicBaseUrl)} on startup!");

            return $"{_publicBaseUrl}/{container.GetKey(path)}";
        }
    }

    extension(string? path)
    {
        public string? ToProfilePhotoUrl() => path.HasValue() ? StorageContainer.ProfilePhotos.GetPublicUrl(path!) : null;
    }
}

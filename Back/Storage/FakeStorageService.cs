using System.Collections.Concurrent;

namespace Estud.Back.Storage;

public class FakeStorageService : IStorageService
{
    public ConcurrentDictionary<string, StorageFileMetadata> Files = new();

    public Task<string> CreatePreSignedUrlForUpload(StorageContainer container, string path, string contentType, long sizeInBytes, TimeSpan expiresIn)
    {
        return Task.FromResult(GetUrl(container, path));
    }

    public Task<string> CreatePreSignedUrlForDownload(StorageContainer container, string path, TimeSpan expiresIn)
    {
        return Task.FromResult(GetUrl(container, path));
    }

    public string GetPublicUrl(StorageContainer container, string path)
    {
        if (!container.IsPublic) throw new InvalidOperationException($"{container} is a private container, use {nameof(CreatePreSignedUrlForDownload)}!");

        return GetUrl(container, path);
    }

    public Task<StorageFileMetadata?> GetMetadata(StorageContainer container, string path)
    {
        return Task.FromResult(Files.GetValueOrDefault(GetKey(container, path)));
    }

    public Task Delete(StorageContainer container, string path)
    {
        Files.TryRemove(GetKey(container, path), out _);
        return Task.CompletedTask;
    }

    public void SimulateUpload(StorageContainer container, string path, string contentType, long sizeInBytes)
    {
        Files[GetKey(container, path)] = new StorageFileMetadata(contentType, sizeInBytes);
    }

    private static string GetUrl(StorageContainer container, string path)
    {
        return $"https://estud.storage.com/{GetKey(container, path)}";
    }

    private static string GetKey(StorageContainer container, string path)
    {
        return $"{container.GetDescription()}/{path}";
    }
}

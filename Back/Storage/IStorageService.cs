namespace Estud.Back.Storage;

public interface IStorageService
{
    Task<string> CreatePreSignedUrlForUpload(StorageContainer container, string path, string contentType, TimeSpan expiresIn);
    Task<string> CreatePreSignedUrlForDownload(StorageContainer container, string path, TimeSpan expiresIn);
    string GetPublicUrl(StorageContainer container, string path);
    Task<StorageFileMetadata?> GetMetadata(StorageContainer container, string path);
    Task Delete(StorageContainer container, string path);
}

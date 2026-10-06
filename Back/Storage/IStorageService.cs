namespace Estud.Back.Storage;

public interface IStorageService
{
    Task<string> CreatePreSignedUrlForUpload(StorageContainer container, string path, string contentType, long sizeInBytes, TimeSpan expiresIn);
    Task<string> CreatePreSignedUrlForDownload(StorageContainer container, string path, TimeSpan expiresIn);
    Task<StorageFileMetadata?> GetMetadata(StorageContainer container, string path);
    Task Delete(StorageContainer container, string path);
}

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;

namespace Estud.Back.Storage;

public class R2StorageService(IAmazonS3 s3, StorageSettings settings) : IStorageService
{
    public async Task<string> CreatePreSignedUrlForUpload(StorageContainer container, string path, string contentType, TimeSpan expiresIn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = GetBucket(container),
            Key = GetKey(container, path),
            Verb = HttpVerb.PUT,
            Protocol = Protocol.HTTPS,
            ContentType = contentType,
            Expires = DateTime.UtcNow.Add(expiresIn),
        };

        return await s3.GetPreSignedURLAsync(request);
    }

    public async Task<string> CreatePreSignedUrlForDownload(StorageContainer container, string path, TimeSpan expiresIn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = GetBucket(container),
            Key = GetKey(container, path),
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS,
            Expires = DateTime.UtcNow.Add(expiresIn),
        };

        return await s3.GetPreSignedURLAsync(request);
    }

    public string GetPublicUrl(StorageContainer container, string path)
    {
        if (!container.IsPublic) throw new InvalidOperationException($"{container} is a private container, use {nameof(CreatePreSignedUrlForDownload)}!");

        return $"{settings.PublicBaseUrl}/{GetKey(container, path)}";
    }

    public async Task<StorageFileMetadata?> GetMetadata(StorageContainer container, string path)
    {
        try
        {
            var response = await s3.GetObjectMetadataAsync(GetBucket(container), GetKey(container, path));
            return new StorageFileMetadata(response.Headers.ContentType, response.Headers.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task Delete(StorageContainer container, string path)
    {
        await s3.DeleteObjectAsync(GetBucket(container), GetKey(container, path));
    }

    private string GetBucket(StorageContainer container)
    {
        return container.IsPublic ? settings.PublicBucket : settings.PrivateBucket;
    }

    private static string GetKey(StorageContainer container, string path)
    {
        return $"{container.GetDescription()}/{path}";
    }
}

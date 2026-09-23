using Amazon.S3;
using Amazon.S3.Model;
using Estud.Back.Settings;
using Testcontainers.Minio;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace Estud.Tests.Base;

public class StorageFactory : IAsyncDisposable
{
    private static readonly HttpClient Http = new();

    private readonly StorageSettings _settings;
    private readonly MinioContainer _container;
    private AmazonS3Client _s3 = null!;

    public StorageFactory()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json");
        _settings = new ConfigurationBuilder().AddJsonFile(configPath).Build().Storage;

        _container = new MinioBuilder("quay.io/minio/minio:latest")
            .WithUsername(_settings.AccessKeyId)
            .WithPassword(_settings.SecretAccessKey)
            .WithPortBinding(new Uri(_settings.ServiceUrl).Port, 9000)
            .WithEnvironment("MINIO_SITE_REGION", "auto")
            .Build();
    }

    public async Task Start()
    {
        await _container.StartAsync();

        var config = new AmazonS3Config
        {
            ForcePathStyle = true,
            AuthenticationRegion = "auto",
            ServiceURL = _settings.ServiceUrl,
        };
        _s3 = new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, config);

        await _s3.PutBucketAsync(_settings.PublicBucket);
        await _s3.PutBucketAsync(_settings.PrivateBucket);

        // Sem o s3:ListBucket o MinIO responde 403 para arquivo inexistente; o R2 público responde 404.
        await _s3.PutBucketPolicyAsync(_settings.PublicBucket, $$"""
        {
            "Version": "2012-10-17",
            "Statement": [
                {
                    "Effect": "Allow",
                    "Principal": { "AWS": ["*"] },
                    "Action": ["s3:GetObject"],
                    "Resource": ["arn:aws:s3:::{{_settings.PublicBucket}}/*"]
                },
                {
                    "Effect": "Allow",
                    "Principal": { "AWS": ["*"] },
                    "Action": ["s3:ListBucket"],
                    "Resource": ["arn:aws:s3:::{{_settings.PublicBucket}}"]
                }
            ]
        }
        """);
    }

    public static async Task<HttpResponseMessage> Upload(string uploadUrl, string contentType, long sizeInBytes)
    {
        using var content = new ByteArrayContent(new byte[sizeInBytes]);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return await Http.PutAsync(uploadUrl, content);
    }

    public static async Task<HttpResponseMessage> Download(string url)
    {
        return await Http.GetAsync(url);
    }

    /// <summary>
    /// Grava direto no bucket, sem passar pela URL pré-assinada — para cenários que ela recusaria (ex.: tipo ou tamanho diferente do declarado).
    /// </summary>
    public async Task Put(StorageContainer container, string path, string contentType, long sizeInBytes)
    {
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = container.IsPublic ? _settings.PublicBucket : _settings.PrivateBucket,
            Key = $"{container.GetDescription()}/{path}",
            ContentType = contentType,
            InputStream = new MemoryStream(new byte[sizeInBytes]),
        });
    }

    public async ValueTask DisposeAsync()
    {
        _s3?.Dispose();
        await _container.DisposeAsync();
    }
}

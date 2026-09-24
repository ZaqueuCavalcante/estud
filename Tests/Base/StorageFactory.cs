using Amazon.S3;
using Amazon.S3.Model;
using Estud.Back.Settings;
using System.Net.Http.Headers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;

namespace Estud.Tests.Base;

public class StorageFactory : IAsyncDisposable
{
    private static readonly HttpClient Http = new();

    private readonly StorageSettings _settings;
    private readonly IContainer _container;
    private AmazonS3Client _s3 = null!;

    public StorageFactory()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json");
        _settings = new ConfigurationBuilder().AddJsonFile(configPath).Build().Storage;

        _container = new ContainerBuilder("rustfs/rustfs:1.0.0")
            .WithEnvironment("RUSTFS_ACCESS_KEY", _settings.AccessKeyId)
            .WithEnvironment("RUSTFS_SECRET_KEY", _settings.SecretAccessKey)
            .WithPortBinding(new Uri(_settings.ServiceUrl).Port, 9000)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(9000).ForPath("/health")))
            .WithName("estud-tests-storage")
            .WithReuse(true)
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

        await EnsureBucket(_settings.PublicBucket);
        await EnsureBucket(_settings.PrivateBucket);

        // Sem o s3:ListBucket o RustFS responde 403 para arquivo inexistente; o R2 público responde 404.
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

    private async Task EnsureBucket(string name)
    {
        try
        {
            await _s3.PutBucketAsync(name);
        }
        catch (BucketAlreadyOwnedByYouException) { }
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

    // O container não é descartado: com reuso, o DisposeAsync do Testcontainers faz StopAsync e derruba ele.
    public ValueTask DisposeAsync()
    {
        _s3?.Dispose();
        return ValueTask.CompletedTask;
    }
}

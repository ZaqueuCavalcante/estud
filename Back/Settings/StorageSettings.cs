namespace Estud.Back.Settings;

public class StorageSettings : SettingsBase
{
    /// <summary>
    /// S3 API endpoint of the R2 account (e.g., https://{accountId}.r2.cloudflarestorage.com).
    /// </summary>
    public string ServiceUrl { get; set; }
    public string AccessKeyId { get; set; }
    public string SecretAccessKey { get; set; }

    /// <summary>
    /// Bucket served by <see cref="PublicBaseUrl"/>, for files anyone with the link may read.
    /// </summary>
    public string PublicBucket { get; set; }

    /// <summary>
    /// Bucket without public access, readable only through pre-signed urls.
    /// </summary>
    public string PrivateBucket { get; set; }

    /// <summary>
    /// Custom domain that serves the public bucket (e.g., https://cdn.estud.com.br).
    /// </summary>
    public string PublicBaseUrl { get; set; }

    public StorageSettings(IConfiguration configuration)
    {
        configuration.GetSection("Storage").Bind(this);

        RequireNonEmpty(ServiceUrl);
        RequireNonEmpty(AccessKeyId);
        RequireNonEmpty(PublicBucket);
        RequireNonEmpty(PrivateBucket);
        RequireNonEmpty(PublicBaseUrl);
        RequireNonEmpty(SecretAccessKey);

        PublicBaseUrl = PublicBaseUrl.TrimEnd('/');
    }
}

public static class StorageSettingsExtensions
{
    extension(IConfiguration configuration)
    {
        public StorageSettings Storage => new(configuration);
    }
}

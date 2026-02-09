namespace Api.Application.Settings;

public class FileStorageSettings
{
    /// <summary>
    /// The storage provider type (MinIO, S3, R2, etc.)
    /// </summary>
    public string Provider { get; set; } = "MinIO";

    /// <summary>
    /// The endpoint URL for the storage service
    /// </summary>
    public string Endpoint { get; set; } = "localhost:9000";

    /// <summary>
    /// Access key / username for authentication
    /// </summary>
    public string AccessKey { get; set; } = "witnes";

    /// <summary>
    /// Secret key / password for authentication
    /// </summary>
    public string SecretKey { get; set; } = "witnes_dev_password";

    /// <summary>
    /// Whether to use SSL/TLS for connections
    /// </summary>
    public bool UseSSL { get; set; } = false;

    /// <summary>
    /// Region for S3-compatible services (e.g., us-east-1, auto)
    /// </summary>
    public string? Region { get; set; }
}

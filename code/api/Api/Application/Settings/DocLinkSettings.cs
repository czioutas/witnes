namespace Api.Application.Settings;

public class DocLinkSettings
{
    /// <summary>
    /// The base URL for the DocLink service
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5001";

    /// <summary>
    /// Timeout in seconds for DocLink API calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum file size in bytes that DocLink can process
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB
}

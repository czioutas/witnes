using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Application.Models;

/// <summary>
/// Model representing a publicly accessible file
/// </summary>
public record PublicFileModel : BaseModel
{
    /// <summary>
    /// Original filename
    /// </summary>
    [Required]
    [JsonPropertyName("original_file_name")]
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// File extension without the dot (e.g., "jpg", "png")
    /// </summary>
    [Required]
    [JsonPropertyName("file_extension")]
    public required string FileExtension { get; set; }

    /// <summary>
    /// MIME type (e.g., "image/jpeg")
    /// </summary>
    [Required]
    [JsonPropertyName("mime_type")]
    public required string MimeType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Required]
    [JsonPropertyName("file_size_bytes")]
    public required long FileSizeBytes { get; set; }

    /// <summary>
    /// Public URL for accessing the file
    /// </summary>
    [Required]
    [JsonPropertyName("public_url")]
    public required string PublicUrl { get; set; }

    /// <summary>
    /// Optional description or alt text
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    [Required]
    [JsonPropertyName("uploaded_at")]
    public required DateTimeOffset UploadedAt { get; set; }
}

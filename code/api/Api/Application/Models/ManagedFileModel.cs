using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Application.Models;

/// <summary>
/// Model representing a managed file with metadata
/// </summary>
public record ManagedFileModel : BaseModel
{
    /// <summary>
    /// Original filename as uploaded by the user (e.g., "organic-certificate.pdf")
    /// </summary>
    [Required]
    [JsonPropertyName("original_file_name")]
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// File extension without the dot (e.g., "pdf", "jpg")
    /// </summary>
    [Required]
    [JsonPropertyName("file_extension")]
    public required string FileExtension { get; set; }

    /// <summary>
    /// MIME type of the file (e.g., "application/pdf", "image/jpeg")
    /// </summary>
    [Required]
    [JsonPropertyName("mime_type")]
    public required string MimeType { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Required]
    [JsonPropertyName("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    [Required]
    [JsonPropertyName("uploaded_at")]
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Optional description or notes about the file
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

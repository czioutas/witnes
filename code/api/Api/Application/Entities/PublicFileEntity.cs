using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.Entities;

/// <summary>
/// Represents a publicly accessible file stored in MinIO/S3.
/// These files are stored in a public bucket and can be accessed without authentication.
/// Used for: tenant logos, product images, DPP assets, etc.
/// </summary>
[Table("public_files")]
public class PublicFileEntity : TenantAwareEntity
{
    /// <summary>
    /// Original filename as uploaded by the user
    /// </summary>
    [Column("original_file_name")]
    [Required]
    [MaxLength(500)]
    public string OriginalFileName { get; set; } = null!;

    /// <summary>
    /// File extension without the dot (e.g., "jpg", "png", "pdf")
    /// </summary>
    [Column("file_extension")]
    [Required]
    [MaxLength(10)]
    public string FileExtension { get; set; } = null!;

    /// <summary>
    /// MIME type (e.g., "image/jpeg", "image/png")
    /// </summary>
    [Column("mime_type")]
    [Required]
    [MaxLength(100)]
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Column("file_size_bytes")]
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Storage path in MinIO/S3 (without bucket name)
    /// Example: {tenantId}/public/{fileId}
    /// </summary>
    [Column("storage_path")]
    [Required]
    [MaxLength(1000)]
    public string StoragePath { get; set; } = null!;

    /// <summary>
    /// Bucket name where the file is stored
    /// Default: "public-files"
    /// </summary>
    [Column("bucket_name")]
    [Required]
    [MaxLength(100)]
    public string BucketName { get; set; } = "public-files";

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    [Column("uploaded_at")]
    [Required]
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Optional description or alt text for the file
    /// </summary>
    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Public URL for accessing the file
    /// Example: https://files.witnes.io/public-files/{tenantId}/public/{fileId}.jpg
    /// </summary>
    [Column("public_url")]
    [Required]
    [MaxLength(2000)]
    public string PublicUrl { get; set; } = null!;
}

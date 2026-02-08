using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Tenancy.Entities;

namespace Api.Application.Entities;

/// <summary>
/// Represents a file managed by the application with metadata tracking.
/// Files are stored in object storage (MinIO/S3) with the GUID as the filename.
/// Storage path pattern: {tenantId}/files/{fileId}
/// </summary>
[Table("managed_files")]
public class ManagedFileEntity : TenantAwareEntity
{
    /// <summary>
    /// Original filename as uploaded by the user (e.g., "organic-certificate.pdf")
    /// </summary>
    [Column("original_file_name")]
    [MaxLength(255)]
    [Required]
    public string OriginalFileName { get; set; } = null!;

    /// <summary>
    /// File extension without the dot (e.g., "pdf", "jpg")
    /// </summary>
    [Column("file_extension")]
    [MaxLength(20)]
    [Required]
    public string FileExtension { get; set; } = null!;

    /// <summary>
    /// MIME type of the file (e.g., "application/pdf", "image/jpeg")
    /// </summary>
    [Column("mime_type")]
    [MaxLength(100)]
    [Required]
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Storage path in object storage (e.g., "tenant123/files/guid456")
    /// The actual filename in storage is just the GUID without extension
    /// </summary>
    [Column("storage_path")]
    [MaxLength(500)]
    [Required]
    public string StoragePath { get; set; } = null!;

    /// <summary>
    /// Bucket name in object storage (e.g., "files")
    /// </summary>
    [Column("bucket_name")]
    [MaxLength(100)]
    [Required]
    public string BucketName { get; set; } = null!;

    /// <summary>
    /// When the file was uploaded
    /// </summary>
    [Column("uploaded_at")]
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// Optional description or notes about the file
    /// </summary>
    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }
}

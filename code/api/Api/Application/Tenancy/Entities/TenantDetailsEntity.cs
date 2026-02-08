using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Entities;
using Libs.Domain;

namespace Api.Application.Tenancy.Entities;

/// <summary>
/// Tenant details that share the same ID as the tenant.
/// Contains business-specific information.
/// </summary>
[Table("tenant_details")]
public class TenantDetailsEntity : TenantAwareEntity
{
    [Column("vat_number")]
    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [Column("company_registration_number")]
    [MaxLength(50)]
    public string? CompanyRegistrationNumber { get; set; }


    [Column("number_of_employees")]
    public int? NumberOfEmployees { get; set; }

    // Address fields
    [Column("street_line_1")]
    [MaxLength(255)]
    public string? StreetLine1 { get; set; }

    [Column("street_line_2")]
    [MaxLength(255)]
    public string? StreetLine2 { get; set; }

    [Column("city")]
    [MaxLength(100)]
    public string? City { get; set; }

    [Column("state_province")]
    [MaxLength(100)]
    public string? StateProvince { get; set; }

    [Column("postal_code")]
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [Column("country")]
    [MaxLength(100)]
    public string? Country { get; set; }

    // Logo
    [Column("logo_file_id")]
    public Guid? LogoFileId { get; set; }

    [ForeignKey(nameof(LogoFileId))]
    public PublicFileEntity? LogoFile { get; set; }
}

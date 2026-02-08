using System.ComponentModel.DataAnnotations;
using Api.Application.Models;
using Libs.Domain;

namespace Api.Application.Tenancy.Models;

public sealed record TenantModel : BaseModel
{
    [Required]
    public required string Identifier { get; set; }
    [Required]
    public required string NormalizedIdentifier { get; set; }

    // TenantDetails fields
    public string? VatNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public int? NumberOfEmployees { get; set; }

    // Address fields
    public string? StreetLine1 { get; set; }
    public string? StreetLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Logo
    public Guid? LogoFileId { get; set; }
    public PublicFileModel? Logo { get; set; }

    // Nested details model for easier access
    public TenantDetailsModel? Details { get; set; }
}

public sealed record TenantDetailsModel
{
    public Guid? LogoFileId { get; set; }
    public PublicFileModel? Logo { get; set; }
}

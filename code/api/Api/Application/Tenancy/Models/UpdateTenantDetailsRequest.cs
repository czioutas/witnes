using System.ComponentModel.DataAnnotations;
using Libs.Domain;

namespace Api.Application.Tenancy.Models;

public sealed record UpdateTenantDetailsRequest
{
    [Required]
    [MaxLength(255)]
    public required string Identifier { get; set; }

    [MaxLength(50)]
    public string? VatNumber { get; set; }

    [MaxLength(50)]
    public string? CompanyRegistrationNumber { get; set; }

    public int? NumberOfEmployees { get; set; }

    // Address fields
    [MaxLength(255)]
    public string? StreetLine1 { get; set; }

    [MaxLength(255)]
    public string? StreetLine2 { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? StateProvince { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }
}

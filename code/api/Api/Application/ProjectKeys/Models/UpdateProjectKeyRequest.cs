using System.ComponentModel.DataAnnotations;

namespace Api.Application.ProjectKeys.Models;

public class UpdateProjectKeyRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<string>? AllowedOrigins { get; set; }

    public bool? IsActive { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Api.Application.ProjectKeys.Models;

public class CreateProjectKeyRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public required string Domain { get; set; }
}

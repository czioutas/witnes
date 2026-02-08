using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Application.Models;

public interface IModel { }

public record BaseModel<TKey> : IModel
{
    [Required]
    [property: JsonPropertyName("id")]
    public required TKey Id { get; set; }

    [Required]
    [property: JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [property: JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

// Non-generic version for backwards compatibility (defaults to Guid)
public record BaseModel : BaseModel<Guid>
{
}

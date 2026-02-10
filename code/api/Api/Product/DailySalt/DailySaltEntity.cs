using System.ComponentModel.DataAnnotations.Schema;
using Api.Application.Application.Entities;

namespace Api.Product.DailySalt;

/// <summary>
/// Stores the daily salt used for visitor hash ID generation.
/// Single row - updated daily at midnight UTC.
/// </summary>
[Table("daily_salt")]
public class DailySaltEntity : AuditableBaseEntity
{
    [Column("salt")]
    public string Salt { get; set; } = null!;

    [Column("active_date")]
    public DateOnly ActiveDate { get; set; }
}

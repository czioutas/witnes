using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Application.Application.Entities
{
    /// <summary>
    /// Base auditable entity.
    /// DO NOT inherit from this generic class directly. Use AuditableBaseEntity (Guid) or AuditableBaseEntityLong (long) instead.
    /// </summary>
    [Obsolete("Do not inherit from AuditableBaseEntity<TKey> directly. Use AuditableBaseEntity for Guid IDs or AuditableBaseEntityLong for long IDs instead. Reflection with generics causes issues with automatic timestamp management.")]
    public abstract class AuditableBaseEntity<TKey> : BaseEntity
    {
        [Key, Column("id")]
        public TKey Id { get; set; } = default!;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        public AuditableBaseEntity()
        {
        }
    }

    /// <summary>
    /// Auditable entity with Guid ID (for backwards compatibility) - application generates GUIDs
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class AuditableBaseEntity : AuditableBaseEntity<Guid>
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    /// <summary>
    /// Auditable entity with long ID (for high-volume entities) - database auto-generates IDs
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public class AuditableBaseEntityLong : AuditableBaseEntity<long>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new long Id { get; set; }
    }
}

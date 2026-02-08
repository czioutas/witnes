using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Application.Application.Entities
{
    public class AuditableBaseWithoutIdEntity : BaseEntity
    {
        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }

        public AuditableBaseWithoutIdEntity()
        {
        }
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Api.Application.Account.Entities;

[Table("AspNetRoles")]
public partial class ApplicationRoleEntity : IdentityRole<Guid>
{
    public ApplicationRoleEntity() { }

    public ApplicationRoleEntity(
        string roleName
    ) : base(roleName)
    {
    }
}

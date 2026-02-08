using Api.Application.Account.Entities;
using Api.Application.Account.Services.Interfaces;
using Api.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Api.Application.Account.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRoleEntity> _roleManager;

    public RoleService(RoleManager<ApplicationRoleEntity> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task AddDefaultSystemRolesAsync()
    {
        string[] roles = [AccountRoles.AdminUserRole.ToString(), AccountRoles.DefaultUserRole.ToString()];

        foreach (var roleName in roles)
        {
            // Check if the role already exists
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                // Create a new role
                ApplicationRoleEntity newRole = new ApplicationRoleEntity(roleName);
                IdentityResult result = await _roleManager.CreateAsync(newRole);

                if (!result.Succeeded)
                {
                    // Handle errors if any
                    throw new Exception($"Failed to create role {roleName}: {string.Join(", ", result.Errors)}");
                }
            }
        }

        return;
    }
}

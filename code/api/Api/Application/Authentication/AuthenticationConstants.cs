namespace Api.Application.Authentication;

public enum AccountRoles
{
    AdminUserRole = 1,
    DefaultUserRole = 2,
}

public static class ClaimTypeConstants
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string EmailVerified = "EmailVerified";
    public const string TenantId = "TenantId";
    public const string DefaultBusinessId = "DefaultBusinessId";
}

namespace Api.Application.Tenancy.Services;

public interface IRequestTenant
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}

public class RequestTenant : IRequestTenant
{
    public Guid TenantId { get; private set; }

    public RequestTenant()
    {
    }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}

using Api.Application.Models;
using Api.Application.Tenancy.Entities;
using Api.Application.Tenancy.Models;
using Api.Data;
using AutoMapper;
using Bogus;
using Libs.Domain;
using Libs.Exceptions;
using Libs.Result;
using Microsoft.EntityFrameworkCore;

namespace Api.Application.Tenancy.Services;

public interface ITenantService
{
    Task<Result<TenantModel>> CreateAsync(string tenantIdentifier);
    Task<TenantModel> FindByNormalizedIdentifierAsync(string normalizedTenantIdentifier);
    Task<Result<TenantModel>> GetAsync(Guid tenantId, Guid userId);
    Task<Result<TenantModel>> GetAsync(Guid tenantId);
    Task<Result<TenantModel>> GetAsync(string normalizedTenantIdentifier, bool bypassFilters = false);
    Task<Result<TenantModel>> UpdateTenantDetailsAsync(Guid tenantId, UpdateTenantDetailsRequest request);
    Task<Result<bool>> UpdateLogoAsync(Guid tenantId, Guid logoFileId);
    Task<int> GetEmployeeCountAsync(Guid tenantId);
    string GetRandomIdentifier();
}

public class TenantService : ITenantService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TenantService> _logger;
    private readonly IMapper _mapper;

    public TenantService(
        IMapper mapper,
        ApplicationDbContext dbContext,
        ILogger<TenantService> logger
    )
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }


    public async Task<Result<TenantModel>> CreateAsync(string tenantIdentifier)
    {
        var normalizedIdentifier = NormalizeIdentifier(tenantIdentifier);
        var tenantEntity = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.NormalizedIdentifier == normalizedIdentifier);

        if (tenantEntity != null)
        {
            return Result<TenantModel>.BusinessError("TenantExists", $"Tenant with Identifier {tenantIdentifier} already exists.");
        }

        var newTenantEntity = new TenantEntity()
        {
            Identifier = tenantIdentifier,
            NormalizedIdentifier = normalizedIdentifier
        };
        await _dbContext.Tenants.AddAsync(newTenantEntity);

        var result = await _dbContext.SaveChangesAsync();

        if (result != 1)
        {
            return Result<TenantModel>.BusinessError("TenantCreation", "Could not create Tenant " + tenantIdentifier);
        }

        // Create default business for the new tenant
        var defaultBusinessName = $"{tenantIdentifier} Business";

        return new Result<TenantModel>(new TenantModel()
        {
            Id = newTenantEntity.Id,
            Identifier = newTenantEntity.Identifier,
            NormalizedIdentifier = newTenantEntity.NormalizedIdentifier
        });
    }

    public async Task<TenantModel> FindByNormalizedIdentifierAsync(string normalizedIdentifier)
    {
        var tenantEntity = await _dbContext.Tenants.FirstAsync(t => t.NormalizedIdentifier == normalizedIdentifier);

        if (tenantEntity != null)
        {
            return new TenantModel()
            {
                Id = tenantEntity.Id,
                Identifier = tenantEntity.Identifier,
                NormalizedIdentifier = tenantEntity.NormalizedIdentifier
            };
        }
        else
        {
            throw new ResourceNotFoundException();
        }
    }

    public async Task<Result<TenantModel>> GetAsync(Guid id, Guid userId)
    {
        TenantEntity? tenantEntity = await _dbContext.Tenants
        .Where(t => t.Id == id)
        .Include(t => t.ApplicationUsers)
        .Where(t => t.ApplicationUsers.FirstOrDefault(u => u.Id == userId) != null)
        .FirstOrDefaultAsync();

        if (tenantEntity is null)
        {
            return Result<TenantModel>.NotFound("Tenant", id.ToString());
        }

        // Get tenant details if they exist
        var tenantDetails = await _dbContext.TenantDetails
            .Where(td => td.TenantId == id)
            .Include(td => td.LogoFile)
            .FirstOrDefaultAsync();

        var model = _mapper.Map<TenantModel>(tenantEntity);

        // Map tenant details if they exist
        if (tenantDetails != null)
        {
            model.VatNumber = tenantDetails.VatNumber;
            model.CompanyRegistrationNumber = tenantDetails.CompanyRegistrationNumber;
            model.NumberOfEmployees = tenantDetails.NumberOfEmployees;
            model.StreetLine1 = tenantDetails.StreetLine1;
            model.StreetLine2 = tenantDetails.StreetLine2;
            model.City = tenantDetails.City;
            model.StateProvince = tenantDetails.StateProvince;
            model.PostalCode = tenantDetails.PostalCode;
            model.Country = tenantDetails.Country;
            model.LogoFileId = tenantDetails.LogoFileId;

            if (tenantDetails.LogoFile != null)
            {
                model.Logo = _mapper.Map<PublicFileModel>(tenantDetails.LogoFile);
            }

            // Also populate Details for controller access
            model.Details = new TenantDetailsModel
            {
                LogoFileId = tenantDetails.LogoFileId,
                Logo = model.Logo
            };
        }

        return new Result<TenantModel>(model);
    }

    public async Task<Result<TenantModel>> GetAsync(Guid tenantId)
    {
        TenantEntity? tenantEntity = await _dbContext.Tenants
        .Where(t => t.Id == tenantId)
        .FirstOrDefaultAsync();

        if (tenantEntity is null)
        {
            return Result<TenantModel>.NotFound("Tenant", tenantId.ToString());
        }

        // Get tenant details if they exist
        var tenantDetails = await _dbContext.TenantDetails
            .Where(td => td.TenantId == tenantId)
            .Include(td => td.LogoFile)
            .FirstOrDefaultAsync();

        var model = _mapper.Map<TenantModel>(tenantEntity);

        // Map tenant details if they exist
        if (tenantDetails != null)
        {
            model.VatNumber = tenantDetails.VatNumber;
            model.CompanyRegistrationNumber = tenantDetails.CompanyRegistrationNumber;
            model.NumberOfEmployees = tenantDetails.NumberOfEmployees;
            model.StreetLine1 = tenantDetails.StreetLine1;
            model.StreetLine2 = tenantDetails.StreetLine2;
            model.City = tenantDetails.City;
            model.StateProvince = tenantDetails.StateProvince;
            model.PostalCode = tenantDetails.PostalCode;
            model.Country = tenantDetails.Country;
            model.LogoFileId = tenantDetails.LogoFileId;

            if (tenantDetails.LogoFile != null)
            {
                model.Logo = _mapper.Map<PublicFileModel>(tenantDetails.LogoFile);
            }

            // Also populate Details for controller access
            model.Details = new TenantDetailsModel
            {
                LogoFileId = tenantDetails.LogoFileId,
                Logo = model.Logo
            };
        }

        return new Result<TenantModel>(model);
    }

    public async Task<Result<TenantModel>> GetAsync(string tenantIdentifier, bool bypassFilters = false)
    {
        var normalizedIdentifier = NormalizeIdentifier(tenantIdentifier);
        var query = _dbContext.Tenants.AsQueryable();

        if (bypassFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        TenantEntity? tenantEntity = await query
            .Where(t => t.NormalizedIdentifier == normalizedIdentifier)
            .FirstOrDefaultAsync();

        if (tenantEntity is null)
        {
            return Result<TenantModel>.NotFound("Tenant", tenantIdentifier);
        }

        return new Result<TenantModel>(_mapper.Map<TenantModel>(tenantEntity));
    }

    public async Task<Result<TenantModel>> UpdateTenantDetailsAsync(Guid tenantId, UpdateTenantDetailsRequest request)
    {
        var tenantEntity = await _dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .FirstOrDefaultAsync();

        if (tenantEntity is null)
        {
            return Result<TenantModel>.NotFound("Tenant", tenantId.ToString());
        }

        // Update tenant identifier
        tenantEntity.Identifier = request.Identifier;

        // Get or create tenant details
        var tenantDetails = await _dbContext.TenantDetails
            .Where(td => td.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (tenantDetails == null)
        {
            tenantDetails = new TenantDetailsEntity
            {
                Id = tenantId,
                TenantId = tenantId
            };
            await _dbContext.TenantDetails.AddAsync(tenantDetails);
        }

        // Update tenant details
        tenantDetails.VatNumber = request.VatNumber;
        tenantDetails.CompanyRegistrationNumber = request.CompanyRegistrationNumber;
        tenantDetails.NumberOfEmployees = request.NumberOfEmployees;
        tenantDetails.StreetLine1 = request.StreetLine1;
        tenantDetails.StreetLine2 = request.StreetLine2;
        tenantDetails.City = request.City;
        tenantDetails.StateProvince = request.StateProvince;
        tenantDetails.PostalCode = request.PostalCode;
        tenantDetails.Country = request.Country;

        await _dbContext.SaveChangesAsync();

        // Return updated tenant model
        return await GetAsync(tenantId);
    }

    public async Task<Result<bool>> UpdateLogoAsync(Guid tenantId, Guid logoFileId)
    {
        var tenantDetails = await _dbContext.TenantDetails
            .Where(td => td.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (tenantDetails == null)
        {
            // Create tenant details if they don't exist
            tenantDetails = new TenantDetailsEntity
            {
                Id = tenantId,
                TenantId = tenantId,
                LogoFileId = logoFileId
            };
            await _dbContext.TenantDetails.AddAsync(tenantDetails);
        }
        else
        {
            tenantDetails.LogoFileId = logoFileId;
        }

        await _dbContext.SaveChangesAsync();

        return new Result<bool>(true);
    }

    public async Task<int> GetEmployeeCountAsync(Guid tenantId)
    {
        var tenantDetails = await _dbContext.TenantDetails
            .Where(td => td.TenantId == tenantId)
            .FirstOrDefaultAsync();

        return tenantDetails?.NumberOfEmployees ?? 0;
    }

    public string GetRandomIdentifier()
    {
        Faker a = new Faker();
        string newRandomIdentifier = a.Address.City() + "-" + a.Address.StreetName() + "-" + a.Name.FirstName() + "-" + a.Name.JobArea().Trim();
        newRandomIdentifier = newRandomIdentifier.Replace(" ", "");
        return newRandomIdentifier;
    }

    private string NormalizeIdentifier(string identifier)
    {
        // Convert to lowercase and replace non-alphanumeric characters with hyphens
        var normalized = identifier.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-")
            .Replace("'", "");

        // Remove any characters that aren't letters, numbers, or hyphens
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[^a-z0-9-]", "-");

        // Replace multiple consecutive hyphens with a single hyphen
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"-+", "-");

        // Remove leading/trailing hyphens
        normalized = normalized.Trim('-');

        return normalized;
    }
}

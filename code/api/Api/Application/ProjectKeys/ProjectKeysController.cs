using Api.Application.ProjectKeys.Models;
using Api.Application.ProjectKeys.Services;
using Api.Application.Tenancy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Application.ProjectKeys;

/// <summary>
/// Controller for managing project keys
/// </summary>
[ApiController]
[Route("api/v1/project-keys")]
[Authorize]
public class ProjectKeysController : ControllerBase
{
    private readonly IProjectKeyService _projectKeyService;
    private readonly IRequestTenant _requestTenant;

    public ProjectKeysController(
        IProjectKeyService projectKeyService,
        IRequestTenant requestTenant)
    {
        _projectKeyService = projectKeyService ?? throw new ArgumentNullException(nameof(projectKeyService));
        _requestTenant = requestTenant ?? throw new ArgumentNullException(nameof(requestTenant));
    }

    /// <summary>
    /// Get all project keys for the current tenant
    /// </summary>
    /// <returns>List of project keys</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectKeyModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProjectKeyModel>>> GetAll()
    {
        var result = await _projectKeyService.GetAllForTenantAsync(_requestTenant.TenantId);
        return !result.IsFailure ? Ok(result.GetValue) : NotFound();
    }

    /// <summary>
    /// Create a new project key for the current tenant
    /// </summary>
    /// <param name="request">Project key creation request</param>
    /// <returns>Created project key</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectKeyModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectKeyModel>> Create([FromBody] CreateProjectKeyRequest request)
    {
        var result = await _projectKeyService.CreateAsync(_requestTenant.TenantId, request);
        return !result.IsFailure ? Ok(result.GetValue) : BadRequest(result);
    }

    /// <summary>
    /// Update a project key
    /// </summary>
    /// <param name="id">Project key ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated project key</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectKeyModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectKeyModel>> Update(Guid id, [FromBody] UpdateProjectKeyRequest request)
    {
        var result = await _projectKeyService.UpdateAsync(id, request);
        return !result.IsFailure ? Ok(result.GetValue) : NotFound();
    }

    /// <summary>
    /// Deactivate a project key
    /// </summary>
    /// <param name="id">Project key ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Deactivate(Guid id)
    {
        var result = await _projectKeyService.DeactivateAsync(id);
        return !result.IsFailure ? NoContent() : NotFound();
    }
}

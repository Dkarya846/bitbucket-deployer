using Microsoft.AspNetCore.Mvc;
using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Services;

namespace BitbucketDeployer.Api.Deployment.Controllers;

[ApiController]
[Route("api/deployments")]
public class DeploymentController(IBitbucketService bitbucketService, IDeploymentService deploymentService) : ControllerBase
{
    // No authentication is wired up yet — this always resolves to "anonymous" until real auth
    // middleware sets HttpContext.User. Kept as a single seam so that wiring is a one-line change.
    private string CurrentUserId => User.Identity?.Name ?? "anonymous";


    /// <summary>Repositories in the configured workspace. Supports search and cursor pagination.</summary>
    [HttpGet("repositories")]
    [ProducesResponseType<PagedResponse<RepositoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RepositoryResponse>>> GetRepositories(
        [FromQuery] string? search, [FromQuery] string? pageToken, CancellationToken cancellationToken)
    {
        var result = await bitbucketService.GetRepositoriesAsync(search, pageToken, cancellationToken);
        return Ok(new PagedResponse<RepositoryResponse>
        {
            Items = result.Items.Select(RepositoryResponse.FromModel).ToList(),
            NextPageToken = result.NextPageToken
        });
    }

    /// <summary>Branches for a single repository. Only called for repositories the user has selected.</summary>
    [HttpGet("repositories/{repository}/branches")]
    [ProducesResponseType<PagedResponse<BranchResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<BranchResponse>>> GetBranches(
        string repository, [FromQuery] string? search, [FromQuery] string? pageToken, CancellationToken cancellationToken)
    {
        var result = await bitbucketService.GetBranchesAsync(repository, search, pageToken, cancellationToken);
        return Ok(new PagedResponse<BranchResponse>
        {
            Items = result.Items.Select(BranchResponse.FromModel).ToList(),
            NextPageToken = result.NextPageToken
        });
    }

    /// <summary>
    /// Custom pipeline definitions for a repository, resolved from bitbucket-pipelines.yml on the
    /// given branch (definitions can differ per branch). `branch` defaults to the repository's main branch.
    /// </summary>
    [HttpGet("repositories/{repository}/pipelines")]
    [ProducesResponseType<List<PipelineResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PipelineResponse>>> GetPipelines(
        string repository, [FromQuery] string? branch, CancellationToken cancellationToken)
    {
        var branchName = branch;
        if (string.IsNullOrWhiteSpace(branchName))
        {
            var repo = await bitbucketService.GetRepositoryAsync(repository, cancellationToken);
            branchName = repo.MainBranch;
        }

        var pipelines = await bitbucketService.GetPipelineDefinitionsAsync(repository, branchName, cancellationToken);
        return Ok(pipelines.Select(PipelineResponse.FromModel).ToList());
    }

    /// <summary>Validates a prospective deployment without triggering anything.</summary>
    [HttpPost("validate")]
    [ProducesResponseType<ValidationResultResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidationResultResponse>> Validate(
        [FromBody] CreateDeploymentRequest request, CancellationToken cancellationToken)
    {
        var result = await deploymentService.ValidateAsync(request, CurrentUserId, cancellationToken);
        return Ok(ValidationResultResponse.FromModel(result));
    }

    /// <summary>Triggers a Bitbucket pipeline for every repository in the request. Re-validates first — no pipeline is triggered if that fails.</summary>
    [HttpPost]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationResultResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeploymentResponse>> CreateDeployment(
        [FromBody] CreateDeploymentRequest request, CancellationToken cancellationToken)
    {
        var deployment = await deploymentService.CreateDeploymentAsync(request, CurrentUserId, cancellationToken);
        return Ok(DeploymentResponse.FromModel(deployment));
    }

    /// <summary>
    /// Deployment history. Not paginated/filterable yet, and limited to whatever the in-memory
    /// store currently holds — no persistent history exists (section 24).
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<DeploymentListItemResponse>>(StatusCodes.Status200OK)]
    public ActionResult<List<DeploymentListItemResponse>> GetHistory()
    {
        var deployments = deploymentService.GetHistory();
        return Ok(deployments.Select(DeploymentListItemResponse.FromModel).ToList());
    }

    /// <summary>Fetches a deployment, refreshing any still-running items' status from Bitbucket first.</summary>
    [HttpGet("{deploymentId}")]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentResponse>> GetDeployment(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = await deploymentService.GetDeploymentAsync(deploymentId, cancellationToken);
        if (deployment is null)
        {
            throw new DeploymentNotFoundException(deploymentId);
        }
        return Ok(DeploymentResponse.FromModel(deployment));
    }

    [HttpPost("{deploymentId}/cancel")]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentResponse>> Cancel(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = await deploymentService.CancelDeploymentAsync(deploymentId, cancellationToken);
        return Ok(DeploymentResponse.FromModel(deployment));
    }

    [HttpPost("{deploymentId}/retry")]
    [ProducesResponseType<DeploymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeploymentResponse>> Retry(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = await deploymentService.RetryDeploymentAsync(deploymentId, cancellationToken);
        return Ok(DeploymentResponse.FromModel(deployment));
    }
}

using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Everything the rest of the application knows about Bitbucket goes through this interface.
/// Implementations own: calling IBitbucketClient, mapping wire DTOs to our own Models, and
/// normalizing Bitbucket-specific vocabulary (statuses, YAML) into application concepts.
/// </summary>
public interface IBitbucketService
{
    Task<PagedResult<Repository>> GetRepositoriesAsync(string? searchTerm, string? pageToken, CancellationToken cancellationToken);

    Task<Repository> GetRepositoryAsync(string repositorySlug, CancellationToken cancellationToken);

    Task<PagedResult<Branch>> GetBranchesAsync(string repositorySlug, string? searchTerm, string? pageToken, CancellationToken cancellationToken);

    Task<Branch> GetBranchAsync(string repositorySlug, string branchName, CancellationToken cancellationToken);

    /// <summary>Available custom pipeline definitions for a repository, resolved from the given branch's bitbucket-pipelines.yml.</summary>
    Task<IReadOnlyList<Pipeline>> GetPipelineDefinitionsAsync(string repositorySlug, string branchName, CancellationToken cancellationToken);

    Task<Pipeline?> GetPipelineDefinitionAsync(string repositorySlug, string branchName, string pipelinePattern, CancellationToken cancellationToken);

    Task<PipelineRunStatus> TriggerPipelineAsync(
        string repositorySlug, string branchName, string pipelinePattern,
        IReadOnlyDictionary<string, string>? variables, CancellationToken cancellationToken);

    Task<PipelineRunStatus> GetPipelineStatusAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken);

    Task CancelPipelineAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken);
}

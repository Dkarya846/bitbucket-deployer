using BitbucketDeployer.Api.Deployment.Clients.Wire;

namespace BitbucketDeployer.Api.Deployment.Clients;

/// <summary>
/// Thin, low-level wrapper over the Bitbucket Cloud REST API. Knows URL shapes, pagination and
/// wire formats only — no business rules, no our-own-model mapping. That mapping lives in
/// BitbucketService. Workspace is taken from BitbucketOptions, never passed in by callers.
/// </summary>
public interface IBitbucketClient
{
    Task<BitbucketPagedResponse<BitbucketRepository>> GetRepositoriesAsync(
        string? searchTerm, string? pageUrl, CancellationToken cancellationToken);

    Task<BitbucketRepository> GetRepositoryAsync(string repositorySlug, CancellationToken cancellationToken);

    Task<BitbucketPagedResponse<BitbucketBranch>> GetBranchesAsync(
        string repositorySlug, string? searchTerm, string? pageUrl, CancellationToken cancellationToken);

    Task<BitbucketBranch> GetBranchAsync(string repositorySlug, string branchName, CancellationToken cancellationToken);

    /// <summary>Raw bitbucket-pipelines.yml contents for a branch, or null if the repo has no pipeline config.</summary>
    Task<string?> GetPipelinesYamlAsync(string repositorySlug, string branchName, CancellationToken cancellationToken);

    Task<BitbucketPagedResponse<BitbucketPipelineRun>> GetPipelineRunsAsync(
        string repositorySlug, string? selectorPattern, int pageLength, CancellationToken cancellationToken);

    Task<BitbucketPipelineRun> TriggerPipelineAsync(
        string repositorySlug, BitbucketTriggerPipelineRequest request, CancellationToken cancellationToken);

    Task<BitbucketPipelineRun> GetPipelineRunAsync(
        string repositorySlug, string pipelineUuid, CancellationToken cancellationToken);

    Task StopPipelineRunAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken);
}

using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

public class BitbucketService(IBitbucketClient client, ILogger<BitbucketService> logger) : IBitbucketService
{
    public async Task<PagedResult<Repository>> GetRepositoriesAsync(string? searchTerm, string? pageToken, CancellationToken cancellationToken)
    {
        var page = await client.GetRepositoriesAsync(searchTerm, pageToken, cancellationToken);
        return new PagedResult<Repository>
        {
            Items = page.Values.Select(MapRepository).ToList(),
            NextPageToken = page.Next
        };
    }

    public async Task<Repository> GetRepositoryAsync(string repositorySlug, CancellationToken cancellationToken)
    {
        var repo = await client.GetRepositoryAsync(repositorySlug, cancellationToken);
        return MapRepository(repo);
    }

    public async Task<PagedResult<Branch>> GetBranchesAsync(string repositorySlug, string? searchTerm, string? pageToken, CancellationToken cancellationToken)
    {
        var page = await client.GetBranchesAsync(repositorySlug, searchTerm, pageToken, cancellationToken);
        return new PagedResult<Branch>
        {
            Items = page.Values.Select(MapBranch).ToList(),
            NextPageToken = page.Next
        };
    }

    public async Task<Branch> GetBranchAsync(string repositorySlug, string branchName, CancellationToken cancellationToken)
    {
        var branch = await client.GetBranchAsync(repositorySlug, branchName, cancellationToken);
        return MapBranch(branch);
    }

    public async Task<IReadOnlyList<Pipeline>> GetPipelineDefinitionsAsync(string repositorySlug, string branchName, CancellationToken cancellationToken)
    {
        var yaml = await client.GetPipelinesYamlAsync(repositorySlug, branchName, cancellationToken);
        if (yaml is null)
        {
            logger.LogInformation(
                "Repository={Repository} Branch={Branch} has no bitbucket-pipelines.yml — no custom pipelines available.",
                repositorySlug, branchName);
            return [];
        }

        var definitions = BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml);
        if (definitions.Count == 0)
        {
            return [];
        }

        // Enrich with last-run metadata; best-effort — pipeline listing failures shouldn't hide definitions.
        var lastRunsByPattern = await TryGetLastRunsByPattern(repositorySlug, cancellationToken);

        return definitions
            .Select(d =>
            {
                lastRunsByPattern.TryGetValue(d.Pattern, out var lastRun);
                return new Pipeline
                {
                    Pattern = d.Pattern,
                    DisplayName = ToDisplayName(d.Pattern),
                    RequiresVariables = d.RequiresVariables,
                    LastRunStatus = lastRun is null ? null : BitbucketStatusMapper.Map(lastRun.State),
                    LastRunAt = lastRun?.CreatedOn
                };
            })
            .ToList();
    }

    public async Task<Pipeline?> GetPipelineDefinitionAsync(string repositorySlug, string branchName, string pipelinePattern, CancellationToken cancellationToken)
    {
        var definitions = await GetPipelineDefinitionsAsync(repositorySlug, branchName, cancellationToken);
        return definitions.FirstOrDefault(d => string.Equals(d.Pattern, pipelinePattern, StringComparison.Ordinal));
    }

    public async Task<PipelineRunStatus> TriggerPipelineAsync(
        string repositorySlug, string branchName, string pipelinePattern,
        IReadOnlyDictionary<string, string>? variables, CancellationToken cancellationToken)
    {
        var request = new BitbucketTriggerPipelineRequest
        {
            Target = new BitbucketTriggerTarget
            {
                RefName = branchName,
                Selector = new BitbucketPipelineSelector { Type = "custom", Pattern = pipelinePattern }
            },
            Variables = variables?.Select(kv => new BitbucketPipelineVariable { Key = kv.Key, Value = kv.Value }).ToList()
        };

        var run = await client.TriggerPipelineAsync(repositorySlug, request, cancellationToken);

        logger.LogInformation(
            "Pipeline triggered. Repository={Repository} Branch={Branch} Pattern={Pattern} PipelineUuid={PipelineUuid}",
            repositorySlug, branchName, pipelinePattern, run.Uuid);

        return MapRunStatus(run);
    }

    public async Task<PipelineRunStatus> GetPipelineStatusAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken)
    {
        var run = await client.GetPipelineRunAsync(repositorySlug, pipelineUuid, cancellationToken);
        return MapRunStatus(run);
    }

    public Task CancelPipelineAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken) =>
        client.StopPipelineRunAsync(repositorySlug, pipelineUuid, cancellationToken);

    private async Task<Dictionary<string, BitbucketPipelineRun>> TryGetLastRunsByPattern(string repositorySlug, CancellationToken cancellationToken)
    {
        try
        {
            var runs = await client.GetPipelineRunsAsync(repositorySlug, selectorPattern: null, pageLength: 50, cancellationToken);
            return runs.Values
                .Where(r => r.Target?.Selector?.Type == "custom" && r.Target.Selector.Pattern is not null)
                .GroupBy(r => r.Target!.Selector!.Pattern!)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedOn).First());
        }
        catch (BitbucketApiException ex)
        {
            logger.LogWarning(ex, "Failed to enrich pipeline definitions with run history for Repository={Repository}", repositorySlug);
            return new Dictionary<string, BitbucketPipelineRun>();
        }
    }

    private static Repository MapRepository(BitbucketRepository r) => new()
    {
        Slug = r.Slug,
        Name = r.Name,
        Description = r.Description,
        MainBranch = r.MainBranch?.Name ?? "main",
        Project = r.Project?.Name,
        UpdatedOn = r.UpdatedOn
    };

    private static Branch MapBranch(BitbucketBranch b) => new()
    {
        Name = b.Name,
        LatestCommitHash = b.Target?.Hash,
        LatestCommitDate = b.Target?.Date
    };

    private static PipelineRunStatus MapRunStatus(BitbucketPipelineRun run) => new()
    {
        PipelineUuid = run.Uuid,
        BuildNumber = run.BuildNumber,
        Status = BitbucketStatusMapper.Map(run.State)
    };

    private static string ToDisplayName(string pattern) =>
        string.Join(' ', pattern.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
}

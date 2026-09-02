using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class PipelineResponse
{
    public required string Pattern { get; init; }
    public required string DisplayName { get; init; }
    public bool RequiresVariables { get; init; }
    public string? LastRunStatus { get; init; }
    public DateTimeOffset? LastRunAt { get; init; }

    public static PipelineResponse FromModel(Pipeline p) => new()
    {
        Pattern = p.Pattern,
        DisplayName = p.DisplayName,
        RequiresVariables = p.RequiresVariables,
        LastRunStatus = p.LastRunStatus?.ToString(),
        LastRunAt = p.LastRunAt
    };
}

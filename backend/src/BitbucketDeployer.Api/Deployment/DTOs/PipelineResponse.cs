using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class PipelineVariableResponse
{
    public required string Name { get; init; }
    public string? Default { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }

    public static PipelineVariableResponse FromModel(PipelineVariable v) => new()
    {
        Name = v.Name,
        Default = v.Default,
        AllowedValues = v.AllowedValues
    };
}

public class PipelineResponse
{
    public required string Pattern { get; init; }
    public required string DisplayName { get; init; }
    public required List<PipelineVariableResponse> Variables { get; init; }
    public string? LastRunStatus { get; init; }
    public DateTimeOffset? LastRunAt { get; init; }

    public static PipelineResponse FromModel(Pipeline p) => new()
    {
        Pattern = p.Pattern,
        DisplayName = p.DisplayName,
        Variables = p.Variables.Select(PipelineVariableResponse.FromModel).ToList(),
        LastRunStatus = p.LastRunStatus?.ToString(),
        LastRunAt = p.LastRunAt
    };
}

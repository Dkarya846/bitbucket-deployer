using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

/// <summary>A single pipeline *run* as returned by GET/POST .../pipelines/ and GET .../pipelines/{uuid}.</summary>
public class BitbucketPipelineRun
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; set; }

    [JsonPropertyName("build_number")]
    public int BuildNumber { get; set; }

    [JsonPropertyName("state")]
    public BitbucketPipelineState? State { get; set; }

    [JsonPropertyName("target")]
    public BitbucketPipelineTarget? Target { get; set; }

    [JsonPropertyName("created_on")]
    public DateTimeOffset? CreatedOn { get; set; }

    [JsonPropertyName("completed_on")]
    public DateTimeOffset? CompletedOn { get; set; }
}

public class BitbucketPipelineState
{
    /// <summary>PENDING | IN_PROGRESS | COMPLETED</summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>Present when Name == IN_PROGRESS. e.g. PENDING | RUNNING | PAUSED | HALTED</summary>
    [JsonPropertyName("stage")]
    public BitbucketNamedValue? Stage { get; set; }

    /// <summary>Present when Name == COMPLETED. e.g. SUCCESSFUL | FAILED | ERROR | STOPPED</summary>
    [JsonPropertyName("result")]
    public BitbucketNamedValue? Result { get; set; }
}

public class BitbucketNamedValue
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class BitbucketPipelineTarget
{
    [JsonPropertyName("ref_type")]
    public string? RefType { get; set; }

    [JsonPropertyName("ref_name")]
    public string? RefName { get; set; }

    [JsonPropertyName("selector")]
    public BitbucketPipelineSelector? Selector { get; set; }
}

public class BitbucketPipelineSelector
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }
}

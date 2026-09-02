using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

/// <summary>Wire shape for POST .../pipelines/. Built exclusively by BitbucketClient — never constructed ad-hoc elsewhere.</summary>
public class BitbucketTriggerPipelineRequest
{
    [JsonPropertyName("target")]
    public required BitbucketTriggerTarget Target { get; set; }

    [JsonPropertyName("variables")]
    public List<BitbucketPipelineVariable>? Variables { get; set; }
}

public class BitbucketTriggerTarget
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "pipeline_ref_target";

    [JsonPropertyName("ref_type")]
    public string RefType { get; set; } = "branch";

    [JsonPropertyName("ref_name")]
    public required string RefName { get; set; }

    [JsonPropertyName("selector")]
    public required BitbucketPipelineSelector Selector { get; set; }
}

public class BitbucketPipelineVariable
{
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [JsonPropertyName("value")]
    public required string Value { get; set; }

    [JsonPropertyName("secured")]
    public bool Secured { get; set; }
}

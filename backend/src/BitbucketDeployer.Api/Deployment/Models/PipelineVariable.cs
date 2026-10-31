namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>One entry from a custom pipeline's `variables:` block in bitbucket-pipelines.yml.</summary>
public class PipelineVariable
{
    public required string Name { get; set; }

    /// <summary>If set, Bitbucket uses this when the trigger request omits the variable — our validation treats it as optional.</summary>
    public string? Default { get; set; }

    /// <summary>A fixed set of choices, if the YAML declares one — the UI should offer a dropdown instead of free text.</summary>
    public IReadOnlyList<string>? AllowedValues { get; set; }
}

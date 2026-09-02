namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>
/// A custom pipeline *definition* (the keys under `pipelines.custom` in bitbucket-pipelines.yml),
/// not a pipeline *run*. See BitbucketService.GetPipelineDefinitionsAsync for how this is derived.
/// </summary>
public class Pipeline
{
    public required string Pattern { get; set; }

    /// <summary>Human-friendly label derived from the pattern, e.g. "deploy-uat" -> "Deploy Uat".</summary>
    public required string DisplayName { get; set; }

    /// <summary>True if the definition declares required variables the user must supply.</summary>
    public bool RequiresVariables { get; set; }

    /// <summary>Result of the most recent run of this pipeline pattern, if any has ever run.</summary>
    public DeploymentStatus? LastRunStatus { get; set; }

    public DateTimeOffset? LastRunAt { get; set; }
}

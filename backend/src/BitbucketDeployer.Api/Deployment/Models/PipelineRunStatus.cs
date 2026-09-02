namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>Normalized status of a single triggered Bitbucket pipeline run.</summary>
public class PipelineRunStatus
{
    public required string PipelineUuid { get; set; }
    public int BuildNumber { get; set; }
    public required DeploymentStatus Status { get; set; }
}

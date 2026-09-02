namespace BitbucketDeployer.Api.Deployment.Models;

public class DeploymentItem
{
    public required string Repository { get; set; }
    public required string Branch { get; set; }
    public required string Pipeline { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
    public string? BitbucketPipelineUuid { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public string? ErrorMessage { get; set; }
}

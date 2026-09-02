namespace BitbucketDeployer.Api.Deployment.DTOs;

public class DeploymentItemRequest
{
    public required string Repository { get; set; }
    public required string Branch { get; set; }
    public required string Pipeline { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
}

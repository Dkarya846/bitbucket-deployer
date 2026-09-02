namespace BitbucketDeployer.Api.Deployment.DTOs;

/// <summary>Shared shape for both POST /validate and POST / (deploy) — same payload, different backend action.</summary>
public class CreateDeploymentRequest
{
    public required string Environment { get; set; }
    public required List<DeploymentItemRequest> Repositories { get; set; }
}

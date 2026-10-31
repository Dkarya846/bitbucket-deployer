using BitbucketDeployer.Api.Deployment.Configuration;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class DeploymentEnvironmentResponse
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool RequiresExtraConfirmation { get; init; }

    public static DeploymentEnvironmentResponse FromOptions(DeploymentEnvironmentOptions options) => new()
    {
        Id = options.Id,
        Name = options.Name,
        RequiresExtraConfirmation = options.RequiresExtraConfirmation
    };
}

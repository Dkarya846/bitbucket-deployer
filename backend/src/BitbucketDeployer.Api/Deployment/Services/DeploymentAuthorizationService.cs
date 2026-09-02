using Microsoft.Extensions.Options;
using BitbucketDeployer.Api.Deployment.Configuration;

namespace BitbucketDeployer.Api.Deployment.Services;

public class DeploymentAuthorizationService(IOptions<AuthorizationOptions> options) : IDeploymentAuthorizationService
{
    private readonly AuthorizationOptions _options = options.Value;

    public bool CanDeploy(string userId, string environment)
    {
        if (!string.Equals(environment, "production", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return _options.ProductionApprovedUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
    }
}

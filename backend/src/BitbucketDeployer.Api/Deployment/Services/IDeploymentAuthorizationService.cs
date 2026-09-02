namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Deployment authorization abstraction (section 22). Kept out of controllers entirely so the
/// eventual real role model (Developer vs. Release Manager, per-environment grants) is a
/// same-interface swap.
/// </summary>
public interface IDeploymentAuthorizationService
{
    bool CanDeploy(string userId, string environment);
}

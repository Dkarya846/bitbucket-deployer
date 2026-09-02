using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Raised when CreateDeploymentAsync's defense-in-depth re-validation fails — the API must never
/// trigger a pipeline off the back of stale/bypassed client-side validation (section 6/17).
/// </summary>
public class DeploymentValidationException(ValidationResult result) : Exception("Deployment validation failed.")
{
    public ValidationResult Result { get; } = result;
}

using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

public interface IDeploymentService
{
    Task<ValidationResult> ValidateAsync(CreateDeploymentRequest request, string userId, CancellationToken cancellationToken);

    /// <summary>Throws DeploymentValidationException if re-validation fails — no pipeline is triggered in that case.</summary>
    Task<Models.Deployment> CreateDeploymentAsync(CreateDeploymentRequest request, string userId, CancellationToken cancellationToken);

    /// <summary>Refreshes any non-terminal items' status from Bitbucket before returning.</summary>
    Task<Models.Deployment?> GetDeploymentAsync(string deploymentId, CancellationToken cancellationToken);

    IReadOnlyList<Models.Deployment> GetHistory();

    Task<Models.Deployment> CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken);

    Task<Models.Deployment> RetryDeploymentAsync(string deploymentId, CancellationToken cancellationToken);
}

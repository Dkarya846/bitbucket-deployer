using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

public interface IDeploymentValidationService
{
    Task<ValidationResult> ValidateAsync(
        string environment, string userId, IReadOnlyList<DeploymentItemRequest> items, CancellationToken cancellationToken);
}

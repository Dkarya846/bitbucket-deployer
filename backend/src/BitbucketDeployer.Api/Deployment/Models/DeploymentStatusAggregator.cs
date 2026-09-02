namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>Derives a deployment's overall status from its items' statuses.</summary>
public static class DeploymentStatusAggregator
{
    public static DeploymentStatus Aggregate(IReadOnlyCollection<DeploymentStatus> itemStatuses)
    {
        if (itemStatuses.Count == 0)
        {
            return DeploymentStatus.Pending;
        }
        if (itemStatuses.Contains(DeploymentStatus.Failed))
        {
            return DeploymentStatus.Failed;
        }
        if (itemStatuses.Contains(DeploymentStatus.Stopped))
        {
            return DeploymentStatus.Stopped;
        }
        if (itemStatuses.Contains(DeploymentStatus.Cancelled))
        {
            return DeploymentStatus.Cancelled;
        }
        if (itemStatuses.All(s => s == DeploymentStatus.Successful))
        {
            return DeploymentStatus.Successful;
        }
        if (itemStatuses.Any(s => s is DeploymentStatus.Running or DeploymentStatus.Queued))
        {
            return DeploymentStatus.Running;
        }
        return DeploymentStatus.Pending;
    }
}

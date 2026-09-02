namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>
/// One user-triggered deployment action, spanning one or more repositories. Held in-memory only
/// (see IDeploymentStore) — Bitbucket remains the source of truth for actual pipeline execution;
/// this record just tracks which pipelines we triggered and their last-known status.
/// </summary>
public class Deployment
{
    public required string Id { get; set; }
    public required string Environment { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public required List<DeploymentItem> Items { get; set; }
}

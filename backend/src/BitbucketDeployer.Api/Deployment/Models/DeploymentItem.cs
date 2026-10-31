namespace BitbucketDeployer.Api.Deployment.Models;

public class DeploymentItem
{
    // App-assigned, not database-generated — SQLite only auto-increments a sole INTEGER PRIMARY
    // KEY, and this is part of a composite key (owner + item) once persisted. Not exposed in any
    // API response; it exists purely so SqliteDeploymentStore has a real, reliable row identity.
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Repository { get; set; }
    public required string Branch { get; set; }
    public required string Pipeline { get; set; }
    public Dictionary<string, string>? Variables { get; set; }
    public string? BitbucketPipelineUuid { get; set; }
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Pending;
    public string? ErrorMessage { get; set; }
}

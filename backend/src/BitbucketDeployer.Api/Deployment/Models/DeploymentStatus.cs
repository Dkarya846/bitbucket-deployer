namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>
/// Normalized deployment/pipeline status. Bitbucket-specific state names are mapped into this
/// enum at the service boundary (see BitbucketStatusMapper) so nothing above the client layer
/// needs to know Bitbucket's vocabulary.
/// </summary>
public enum DeploymentStatus
{
    Pending,
    Validating,
    Queued,
    Running,
    Successful,
    Failed,
    Stopped,
    Cancelled
}

public static class DeploymentStatusExtensions
{
    private static readonly DeploymentStatus[] TerminalStatuses =
    [
        DeploymentStatus.Successful,
        DeploymentStatus.Failed,
        DeploymentStatus.Stopped,
        DeploymentStatus.Cancelled
    ];

    public static bool IsTerminal(this DeploymentStatus status) => TerminalStatuses.Contains(status);
}

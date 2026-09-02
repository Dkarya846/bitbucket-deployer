using BitbucketDeployer.Api.Deployment.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Translates Bitbucket's state/stage/result vocabulary into our own DeploymentStatus enum.
/// This is the only place in the codebase that should know Bitbucket's status names.
/// </summary>
public static class BitbucketStatusMapper
{
    public static DeploymentStatus Map(BitbucketPipelineState? state)
    {
        if (state is null)
        {
            return DeploymentStatus.Pending;
        }

        return state.Name.ToUpperInvariant() switch
        {
            "PENDING" => DeploymentStatus.Queued,
            "IN_PROGRESS" => MapInProgress(state.Stage?.Name),
            "COMPLETED" => MapCompleted(state.Result?.Name),
            _ => DeploymentStatus.Pending
        };
    }

    private static DeploymentStatus MapInProgress(string? stageName) => stageName?.ToUpperInvariant() switch
    {
        "PENDING" => DeploymentStatus.Queued,
        "RUNNING" or "PAUSED" or "HALTED" => DeploymentStatus.Running,
        _ => DeploymentStatus.Running
    };

    private static DeploymentStatus MapCompleted(string? resultName) => resultName?.ToUpperInvariant() switch
    {
        "SUCCESSFUL" => DeploymentStatus.Successful,
        "STOPPED" => DeploymentStatus.Stopped,
        "FAILED" or "ERROR" => DeploymentStatus.Failed,
        _ => DeploymentStatus.Failed
    };
}

namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Persistence for deployment records. Bitbucket remains the source of truth for actual pipeline
/// execution — this just tracks which pipelines we triggered and their last-known status, so
/// backend restarts don't lose in-flight deployment history (see SqliteDeploymentStore).
/// </summary>
public interface IDeploymentStore
{
    Models.Deployment Add(Models.Deployment deployment);
    Models.Deployment? Get(string id);
    IReadOnlyList<Models.Deployment> GetAll();
    string GenerateNextId();

    /// <summary>
    /// Persists in-place mutations made to a deployment/its items after it was returned by Add/Get
    /// (e.g. status changes from a Bitbucket poll). A no-op for a store that's already the live
    /// object graph (InMemoryDeploymentStore); required for a DB-backed store to flush changes.
    /// </summary>
    Task SaveAsync(Models.Deployment deployment, CancellationToken cancellationToken);
}

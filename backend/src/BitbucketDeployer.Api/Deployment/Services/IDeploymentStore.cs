namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// In-memory-only for now (section 1: minimize persistent storage, Bitbucket is the source of
/// truth for execution state). Swapping this for a MongoDB-backed implementation later requires
/// no change to IDeploymentService or the API contract — only this interface's implementation.
/// </summary>
public interface IDeploymentStore
{
    Models.Deployment Add(Models.Deployment deployment);
    Models.Deployment? Get(string id);
    IReadOnlyList<Models.Deployment> GetAll();
    string GenerateNextId();
}

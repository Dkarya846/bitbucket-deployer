using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class RepositoryResponse
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string MainBranch { get; init; }
    public string? Project { get; init; }

    public static RepositoryResponse FromModel(Repository r) => new()
    {
        Slug = r.Slug,
        Name = r.Name,
        Description = r.Description,
        MainBranch = r.MainBranch,
        Project = r.Project
    };
}

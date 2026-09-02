using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class BranchResponse
{
    public required string Name { get; init; }
    public string? LatestCommitHash { get; init; }
    public DateTimeOffset? LatestCommitDate { get; init; }

    public static BranchResponse FromModel(Branch b) => new()
    {
        Name = b.Name,
        LatestCommitHash = b.LatestCommitHash,
        LatestCommitDate = b.LatestCommitDate
    };
}

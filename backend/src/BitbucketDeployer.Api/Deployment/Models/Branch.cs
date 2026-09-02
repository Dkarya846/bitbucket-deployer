namespace BitbucketDeployer.Api.Deployment.Models;

public class Branch
{
    public required string Name { get; set; }
    public string? LatestCommitHash { get; set; }
    public DateTimeOffset? LatestCommitDate { get; set; }
}

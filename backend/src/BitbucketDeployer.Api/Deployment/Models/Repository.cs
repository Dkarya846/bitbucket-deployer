namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>Our own repository representation — decoupled from Bitbucket's wire format.</summary>
public class Repository
{
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string MainBranch { get; set; }
    public string? Project { get; set; }
    public DateTimeOffset? UpdatedOn { get; set; }
}

namespace BitbucketDeployer.Api.Deployment.Models;

/// <summary>
/// NextPageToken is an opaque cursor (Bitbucket's absolute "next" URL) the client echoes back
/// unchanged to fetch the following page. It is null once the last page has been reached.
/// </summary>
public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public string? NextPageToken { get; init; }
}

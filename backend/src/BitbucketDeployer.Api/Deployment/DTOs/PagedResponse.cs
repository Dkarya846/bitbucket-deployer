namespace BitbucketDeployer.Api.Deployment.DTOs;

public class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Opaque cursor — pass back unchanged as `pageToken` to fetch the next page. Null on the last page.</summary>
    public string? NextPageToken { get; init; }
}

using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

/// <summary>Generic envelope used by every Bitbucket Cloud v2.0 list endpoint.</summary>
public class BitbucketPagedResponse<T>
{
    [JsonPropertyName("pagelen")]
    public int PageLength { get; set; }

    [JsonPropertyName("size")]
    public int? Size { get; set; }

    [JsonPropertyName("page")]
    public int? Page { get; set; }

    /// <summary>Full URL to the next page, or null if this is the last page.</summary>
    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("values")]
    public List<T> Values { get; set; } = [];
}

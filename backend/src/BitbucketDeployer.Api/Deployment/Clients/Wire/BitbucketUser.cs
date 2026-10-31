using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

/// <summary>The account the configured API token belongs to (GET /2.0/user — no workspace in the path).</summary>
public class BitbucketUser
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

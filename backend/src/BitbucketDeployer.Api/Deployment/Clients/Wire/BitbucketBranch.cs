using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

public class BitbucketBranch
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("target")]
    public BitbucketCommit? Target { get; set; }
}

public class BitbucketCommit
{
    [JsonPropertyName("hash")]
    public string? Hash { get; set; }

    [JsonPropertyName("date")]
    public DateTimeOffset? Date { get; set; }
}

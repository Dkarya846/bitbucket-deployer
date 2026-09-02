using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

public class BitbucketRepository
{
    [JsonPropertyName("slug")]
    public required string Slug { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("mainbranch")]
    public BitbucketMainBranch? MainBranch { get; set; }

    [JsonPropertyName("project")]
    public BitbucketProject? Project { get; set; }

    [JsonPropertyName("updated_on")]
    public DateTimeOffset? UpdatedOn { get; set; }
}

public class BitbucketMainBranch
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class BitbucketProject
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

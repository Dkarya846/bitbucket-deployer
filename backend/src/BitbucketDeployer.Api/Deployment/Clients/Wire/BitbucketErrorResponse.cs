using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Deployment.Clients.Wire;

/// <summary>
/// Bitbucket uses at least two error envelope shapes: the standard REST API
/// `{"error": {"message": "..."}}`, and a flatter `{"code": ..., "message": "..."}` seen from
/// some 5xx responses (e.g. pipeline trigger failures). Both are optional here so either can bind.
/// </summary>
public class BitbucketErrorResponse
{
    [JsonPropertyName("error")]
    public BitbucketErrorDetail? Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class BitbucketErrorDetail
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

using System.Text.Json.Serialization;

namespace BitbucketDeployer.Api.Auth.Clients.Wire;

/// <summary>Response from POST https://bitbucket.org/site/oauth2/access_token.</summary>
public class BitbucketOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresInSeconds { get; set; }
}

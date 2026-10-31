namespace BitbucketDeployer.Api.Auth.Models;

/// <summary>
/// One logged-in browser session. Id is the opaque value stored in the session cookie — never the
/// token itself. AccessToken/RefreshToken are encrypted at rest via IDataProtector (see
/// OAuthSessionStore) before being written to SQLite.
/// </summary>
public class OAuthSession
{
    public required string Id { get; set; }
    public required string EncryptedAccessToken { get; set; }
    public required string EncryptedRefreshToken { get; set; }
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

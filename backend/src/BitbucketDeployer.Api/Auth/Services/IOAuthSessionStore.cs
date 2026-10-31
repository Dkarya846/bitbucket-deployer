namespace BitbucketDeployer.Api.Auth.Services;

/// <summary>Decrypted, in-memory view of a session — the persisted OAuthSession entity only ever holds encrypted tokens.</summary>
public record DecryptedSession(
    string SessionId, string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, string DisplayName, string Username);

public interface IOAuthSessionStore
{
    Task<DecryptedSession> CreateAsync(
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt,
        string displayName, string username, CancellationToken cancellationToken);

    Task<DecryptedSession?> GetAsync(string sessionId, CancellationToken cancellationToken);

    Task UpdateTokensAsync(
        string sessionId, string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt, CancellationToken cancellationToken);

    Task DeleteAsync(string sessionId, CancellationToken cancellationToken);
}

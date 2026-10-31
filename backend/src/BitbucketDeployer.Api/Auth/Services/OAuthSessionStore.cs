using Microsoft.AspNetCore.DataProtection;
using BitbucketDeployer.Api.Auth.Models;
using BitbucketDeployer.Api.Deployment.Persistence;

namespace BitbucketDeployer.Api.Auth.Services;

/// <summary>
/// Access/refresh tokens are encrypted at rest with ASP.NET Core's Data Protection APIs before
/// being written to SQLite — the DB file is already git-ignored, but there's no reason to store
/// live Bitbucket credentials in plaintext when encryption is this cheap.
/// </summary>
public class OAuthSessionStore(DeploymentDbContext context, IDataProtectionProvider dataProtectionProvider) : IOAuthSessionStore
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("BitbucketDeployer.OAuthSession.v1");

    public async Task<DecryptedSession> CreateAsync(
        string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt,
        string displayName, string username, CancellationToken cancellationToken)
    {
        var session = new OAuthSession
        {
            Id = Guid.NewGuid().ToString("N"),
            EncryptedAccessToken = _protector.Protect(accessToken),
            EncryptedRefreshToken = _protector.Protect(refreshToken),
            AccessTokenExpiresAt = accessTokenExpiresAt,
            DisplayName = displayName,
            Username = username,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.OAuthSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        return new DecryptedSession(session.Id, accessToken, refreshToken, accessTokenExpiresAt, displayName, username);
    }

    public async Task<DecryptedSession?> GetAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await context.OAuthSessions.FindAsync([sessionId], cancellationToken);
        if (session is null)
        {
            return null;
        }

        return new DecryptedSession(
            session.Id,
            _protector.Unprotect(session.EncryptedAccessToken),
            _protector.Unprotect(session.EncryptedRefreshToken),
            session.AccessTokenExpiresAt,
            session.DisplayName,
            session.Username);
    }

    public async Task UpdateTokensAsync(
        string sessionId, string accessToken, string refreshToken, DateTimeOffset accessTokenExpiresAt, CancellationToken cancellationToken)
    {
        var session = await context.OAuthSessions.FindAsync([sessionId], cancellationToken)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        session.EncryptedAccessToken = _protector.Protect(accessToken);
        session.EncryptedRefreshToken = _protector.Protect(refreshToken);
        session.AccessTokenExpiresAt = accessTokenExpiresAt;

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await context.OAuthSessions.FindAsync([sessionId], cancellationToken);
        if (session is not null)
        {
            context.OAuthSessions.Remove(session);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

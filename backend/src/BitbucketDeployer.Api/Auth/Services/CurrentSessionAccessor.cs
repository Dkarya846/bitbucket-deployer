using Microsoft.AspNetCore.Http;
using BitbucketDeployer.Api.Auth.Clients;

namespace BitbucketDeployer.Api.Auth.Services;

public class CurrentSessionAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOAuthSessionStore sessionStore,
    IBitbucketOAuthClient oauthClient,
    ILogger<CurrentSessionAccessor> logger) : ICurrentSessionAccessor
{
    public const string CookieName = "bbd_session";

    // Refresh a little before the real expiry so a request's actual Bitbucket call never races it.
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    public async Task<DecryptedSession?> GetCurrentSessionAsync(CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null || !httpContext.Request.Cookies.TryGetValue(CookieName, out var sessionId) || string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        var session = await sessionStore.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        if (session.AccessTokenExpiresAt - RefreshSkew > DateTimeOffset.UtcNow)
        {
            return session;
        }

        try
        {
            var refreshed = await oauthClient.RefreshAsync(session.RefreshToken, cancellationToken);
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshed.ExpiresInSeconds);
            await sessionStore.UpdateTokensAsync(session.SessionId, refreshed.AccessToken, refreshed.RefreshToken, expiresAt, cancellationToken);

            return session with { AccessToken = refreshed.AccessToken, RefreshToken = refreshed.RefreshToken, AccessTokenExpiresAt = expiresAt };
        }
        catch (BitbucketOAuthException ex)
        {
            logger.LogWarning(ex, "Failed to refresh Bitbucket session {SessionId}; treating as logged out.", session.SessionId);
            await sessionStore.DeleteAsync(session.SessionId, cancellationToken);
            return null;
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken) =>
        (await GetCurrentSessionAsync(cancellationToken))?.AccessToken ?? throw new NotAuthenticatedException();

    public async Task<string> GetDisplayNameAsync(CancellationToken cancellationToken) =>
        (await GetCurrentSessionAsync(cancellationToken))?.DisplayName ?? throw new NotAuthenticatedException();
}

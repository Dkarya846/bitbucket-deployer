namespace BitbucketDeployer.Api.Auth.Services;

/// <summary>Resolves the current HTTP request's logged-in Bitbucket session, transparently refreshing an expired access token.</summary>
public interface ICurrentSessionAccessor
{
    Task<DecryptedSession?> GetCurrentSessionAsync(CancellationToken cancellationToken);

    /// <summary>Throws NotAuthenticatedException if no valid session exists.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);

    /// <summary>Throws NotAuthenticatedException if no valid session exists.</summary>
    Task<string> GetDisplayNameAsync(CancellationToken cancellationToken);
}

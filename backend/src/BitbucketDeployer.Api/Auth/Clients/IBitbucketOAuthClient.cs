using BitbucketDeployer.Api.Auth.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Clients.Wire;

namespace BitbucketDeployer.Api.Auth.Clients;

/// <summary>
/// Talks to bitbucket.org/site/oauth2/* (OAuth) and api.bitbucket.org/2.0/user (profile lookup
/// right after login) — distinct from IBitbucketClient, which talks to api.bitbucket.org on
/// behalf of an already-established session and requires one to exist.
/// </summary>
public interface IBitbucketOAuthClient
{
    Task<BitbucketOAuthTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
    Task<BitbucketOAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task<BitbucketUser> GetUserAsync(string accessToken, CancellationToken cancellationToken);
}

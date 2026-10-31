using System.Net.Http.Headers;
using BitbucketDeployer.Api.Auth.Services;

namespace BitbucketDeployer.Api.Deployment.Clients;

/// <summary>
/// Attaches the current request's logged-in user's Bitbucket access token to every outgoing
/// request. Throws NotAuthenticatedException (before sending) if no one is logged in — centralized
/// here so no service code has to check login state itself.
/// </summary>
public class BitbucketAuthHandler(ICurrentSessionAccessor currentSessionAccessor) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await currentSessionAccessor.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await base.SendAsync(request, cancellationToken);
    }
}

using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using BitbucketDeployer.Api.Deployment.Configuration;

namespace BitbucketDeployer.Api.Deployment.Clients;

/// <summary>
/// Attaches the Bitbucket API token to every outgoing request. Centralized here so the token
/// never has to be threaded through service code, and can never accidentally end up in a log line.
/// </summary>
public class BitbucketAuthHandler(IOptions<BitbucketOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = options.Value.ApiToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

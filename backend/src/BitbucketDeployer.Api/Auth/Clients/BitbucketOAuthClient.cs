using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using BitbucketDeployer.Api.Auth.Clients.Wire;
using BitbucketDeployer.Api.Auth.Configuration;
using BitbucketDeployer.Api.Auth.Services;
using BitbucketDeployer.Api.Deployment.Clients.Wire;

namespace BitbucketDeployer.Api.Auth.Clients;

public class BitbucketOAuthClient(IHttpClientFactory httpClientFactory, IOptions<BitbucketOAuthOptions> options, ILogger<BitbucketOAuthClient> logger)
    : IBitbucketOAuthClient
{
    private const string UserProfileUrl = "https://api.bitbucket.org/2.0/user";
    private readonly BitbucketOAuthOptions _options = options.Value;

    public Task<BitbucketOAuthTokenResponse> ExchangeCodeAsync(string code, CancellationToken cancellationToken) =>
        RequestTokenAsync(new Dictionary<string, string> { ["grant_type"] = "authorization_code", ["code"] = code }, cancellationToken);

    public Task<BitbucketOAuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        RequestTokenAsync(new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = refreshToken }, cancellationToken);

    public async Task<BitbucketUser> GetUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UserProfileUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Bitbucket user profile request failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new BitbucketOAuthException("Failed to fetch the Bitbucket user profile.");
        }

        return await response.Content.ReadFromJsonAsync<BitbucketUser>(cancellationToken: cancellationToken)
               ?? throw new BitbucketOAuthException("Bitbucket returned an empty user profile response.");
    }

    private async Task<BitbucketOAuthTokenResponse> RequestTokenAsync(Dictionary<string, string> form, CancellationToken cancellationToken)
    {
        using var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl) { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}")));

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Bitbucket OAuth token request failed with {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new BitbucketOAuthException("Failed to exchange the authorization code with Bitbucket.");
        }

        return JsonSerializer.Deserialize<BitbucketOAuthTokenResponse>(body)
               ?? throw new BitbucketOAuthException("Bitbucket returned an empty token response.");
    }
}

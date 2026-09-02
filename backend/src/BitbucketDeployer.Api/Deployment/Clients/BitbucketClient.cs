using System.Net;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Options;
using BitbucketDeployer.Api.Deployment.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Configuration;

namespace BitbucketDeployer.Api.Deployment.Clients;

public class BitbucketClient(HttpClient httpClient, IOptions<BitbucketOptions> options, ILogger<BitbucketClient> logger)
    : IBitbucketClient
{
    private readonly BitbucketOptions _options = options.Value;

    public Task<BitbucketPagedResponse<BitbucketRepository>> GetRepositoriesAsync(
        string? searchTerm, string? pageUrl, CancellationToken cancellationToken)
    {
        if (pageUrl is not null)
        {
            return GetAsync<BitbucketPagedResponse<BitbucketRepository>>(pageUrl, cancellationToken);
        }

        var query = new Dictionary<string, string?>
        {
            ["pagelen"] = _options.DefaultPageLength.ToString(),
            ["sort"] = "-updated_on"
        };
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query["q"] = $"name~\"{EscapeBbql(searchTerm)}\"";
        }

        var path = BuildPath($"repositories/{_options.Workspace}", query);
        return GetAsync<BitbucketPagedResponse<BitbucketRepository>>(path, cancellationToken);
    }

    public Task<BitbucketRepository> GetRepositoryAsync(string repositorySlug, CancellationToken cancellationToken) =>
        GetAsync<BitbucketRepository>($"repositories/{_options.Workspace}/{repositorySlug}", cancellationToken);

    public Task<BitbucketPagedResponse<BitbucketBranch>> GetBranchesAsync(
        string repositorySlug, string? searchTerm, string? pageUrl, CancellationToken cancellationToken)
    {
        if (pageUrl is not null)
        {
            return GetAsync<BitbucketPagedResponse<BitbucketBranch>>(pageUrl, cancellationToken);
        }

        var query = new Dictionary<string, string?>
        {
            ["pagelen"] = _options.DefaultPageLength.ToString(),
            ["sort"] = "-target.date"
        };
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query["q"] = $"name~\"{EscapeBbql(searchTerm)}\"";
        }

        var path = BuildPath($"repositories/{_options.Workspace}/{repositorySlug}/refs/branches", query);
        return GetAsync<BitbucketPagedResponse<BitbucketBranch>>(path, cancellationToken);
    }

    public Task<BitbucketBranch> GetBranchAsync(string repositorySlug, string branchName, CancellationToken cancellationToken) =>
        GetAsync<BitbucketBranch>(
            $"repositories/{_options.Workspace}/{repositorySlug}/refs/branches/{Uri.EscapeDataString(branchName)}",
            cancellationToken);

    public async Task<string?> GetPipelinesYamlAsync(string repositorySlug, string branchName, CancellationToken cancellationToken)
    {
        var path = $"repositories/{_options.Workspace}/{repositorySlug}/src/{Uri.EscapeDataString(branchName)}/bitbucket-pipelines.yml";
        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccess(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public Task<BitbucketPagedResponse<BitbucketPipelineRun>> GetPipelineRunsAsync(
        string repositorySlug, string? selectorPattern, int pageLength, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["pagelen"] = pageLength.ToString(),
            ["sort"] = "-created_on"
        };
        if (!string.IsNullOrWhiteSpace(selectorPattern))
        {
            query["q"] = $"target.selector.pattern=\"{EscapeBbql(selectorPattern)}\"";
        }

        var path = BuildPath($"repositories/{_options.Workspace}/{repositorySlug}/pipelines/", query);
        return GetAsync<BitbucketPagedResponse<BitbucketPipelineRun>>(path, cancellationToken);
    }

    public async Task<BitbucketPipelineRun> TriggerPipelineAsync(
        string repositorySlug, BitbucketTriggerPipelineRequest request, CancellationToken cancellationToken)
    {
        var path = $"repositories/{_options.Workspace}/{repositorySlug}/pipelines/";
        using var response = await httpClient.PostAsJsonAsync(path, request, cancellationToken);
        await EnsureSuccess(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<BitbucketPipelineRun>(cancellationToken: cancellationToken)
               ?? throw new BitbucketApiException(response.StatusCode, "Bitbucket returned an empty trigger response.");
    }

    public Task<BitbucketPipelineRun> GetPipelineRunAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken) =>
        GetAsync<BitbucketPipelineRun>(
            $"repositories/{_options.Workspace}/{repositorySlug}/pipelines/{pipelineUuid}",
            cancellationToken);

    public async Task StopPipelineRunAsync(string repositorySlug, string pipelineUuid, CancellationToken cancellationToken)
    {
        var path = $"repositories/{_options.Workspace}/{repositorySlug}/pipelines/{pipelineUuid}/stopPipeline";
        using var response = await httpClient.PostAsync(path, content: null, cancellationToken);
        await EnsureSuccess(response, cancellationToken);
    }

    private async Task<T> GetAsync<T>(string pathOrAbsoluteUrl, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(pathOrAbsoluteUrl, cancellationToken);
        await EnsureSuccess(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
               ?? throw new BitbucketApiException(response.StatusCode, "Bitbucket returned an empty response body.");
    }

    private async Task EnsureSuccess(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
        string? bitbucketMessage = null;
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<BitbucketErrorResponse>(rawBody);
            bitbucketMessage = parsed?.Error?.Message ?? parsed?.Message;
        }
        catch (System.Text.Json.JsonException)
        {
            // Bitbucket occasionally returns non-JSON error bodies (e.g. plain-text 5xx from its edge).
        }

        logger.LogWarning(
            "Bitbucket API call failed with {StatusCode}: {Message}. RequestUri={RequestUri} RawBody={RawBody}",
            (int)response.StatusCode, bitbucketMessage ?? "(no message)", response.RequestMessage?.RequestUri,
            string.IsNullOrEmpty(rawBody) ? "(empty)" : rawBody);

        throw new BitbucketApiException(
            response.StatusCode,
            bitbucketMessage ?? $"Bitbucket returned HTTP {(int)response.StatusCode}.",
            rawBody);
    }

    private static string BuildPath(string basePath, Dictionary<string, string?> query)
    {
        var queryString = HttpUtility.ParseQueryString(string.Empty);
        foreach (var (key, value) in query)
        {
            if (value is not null)
            {
                queryString[key] = value;
            }
        }

        return $"{basePath}?{queryString}";
    }

    private static string EscapeBbql(string value) => value.Replace("\"", "\\\"");
}

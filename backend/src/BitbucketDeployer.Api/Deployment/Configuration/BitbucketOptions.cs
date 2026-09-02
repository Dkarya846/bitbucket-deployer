namespace BitbucketDeployer.Api.Deployment.Configuration;

/// <summary>
/// Bitbucket Cloud connection settings. Populate ApiToken from user-secrets/environment
/// variables/a secret manager in real environments — never commit it to appsettings.json.
/// </summary>
public class BitbucketOptions
{
    public const string SectionName = "Bitbucket";

    public required string Workspace { get; set; }

    /// <summary>Must end with a trailing slash — HttpClient.BaseAddress silently drops the path otherwise.</summary>
    public string BaseUrl { get; set; } = "https://api.bitbucket.org/2.0/";

    /// <summary>Bitbucket Cloud API token (Bearer auth), scoped to repository/pipeline read+write.</summary>
    public string ApiToken { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Default page size requested from Bitbucket list endpoints.</summary>
    public int DefaultPageLength { get; set; } = 25;
}

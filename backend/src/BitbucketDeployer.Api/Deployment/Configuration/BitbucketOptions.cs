namespace BitbucketDeployer.Api.Deployment.Configuration;

/// <summary>
/// Bitbucket Cloud connection settings. Authentication is per-user via OAuth (see
/// BitbucketOAuthOptions) — there is no shared app-wide token anymore.
/// </summary>
public class BitbucketOptions
{
    public const string SectionName = "Bitbucket";

    public required string Workspace { get; set; }

    /// <summary>Must end with a trailing slash — HttpClient.BaseAddress silently drops the path otherwise.</summary>
    public string BaseUrl { get; set; } = "https://api.bitbucket.org/2.0/";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Default page size requested from Bitbucket list endpoints.</summary>
    public int DefaultPageLength { get; set; } = 25;
}

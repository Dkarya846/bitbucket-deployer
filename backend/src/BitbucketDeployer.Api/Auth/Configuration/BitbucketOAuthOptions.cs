namespace BitbucketDeployer.Api.Auth.Configuration;

/// <summary>
/// Bitbucket OAuth consumer settings. Create the consumer at either
/// bitbucket.org/{workspace}/workspace/settings/oauth-consumers (needs workspace admin) or
/// bitbucket.org/account/settings/oauth-consumers/ (personal account, no admin rights needed —
/// use this one if you don't have workspace admin access). Either way: callback URL
/// "{backend base url}/api/auth/callback", Repositories/Pipelines read+write permissions.
/// ClientSecret must come from user-secrets/environment variables — never appsettings.json.
/// </summary>
public class BitbucketOAuthOptions
{
    public const string SectionName = "Bitbucket:OAuth";

    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }

    public string AuthorizeUrl { get; set; } = "https://bitbucket.org/site/oauth2/authorize";
    public string TokenUrl { get; set; } = "https://bitbucket.org/site/oauth2/access_token";
}

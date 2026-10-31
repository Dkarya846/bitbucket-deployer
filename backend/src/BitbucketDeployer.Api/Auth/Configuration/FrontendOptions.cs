namespace BitbucketDeployer.Api.Auth.Configuration;

/// <summary>Where to send the browser back to once the OAuth callback has set the session cookie.</summary>
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = "http://localhost:4200";
}

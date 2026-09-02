namespace BitbucketDeployer.Api.Deployment.Configuration;

/// <summary>
/// Placeholder role model until real authentication/roles are wired in. Everyone can deploy to
/// non-production environments; production requires being on this explicit allow-list. Replace
/// with a proper role-based lookup (Developer/Release Manager, per section 22) without touching
/// IDeploymentAuthorizationService's callers.
/// </summary>
public class AuthorizationOptions
{
    public const string SectionName = "Authorization";
    public List<string> ProductionApprovedUsers { get; set; } = [];
}

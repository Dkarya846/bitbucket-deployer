namespace BitbucketDeployer.Api.Deployment.Configuration;

/// <summary>One selectable deployment target (UAT / Staging / Production), as configured in appsettings.</summary>
public class DeploymentEnvironmentOptions
{
    public const string SectionName = "Environments";

    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool RequiresExtraConfirmation { get; set; }
}

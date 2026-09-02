namespace BitbucketDeployer.Api.Deployment.Services;

public class DeploymentNotFoundException(string deploymentId) : Exception($"Deployment '{deploymentId}' was not found.");

using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

public class DeploymentValidationService(
    IBitbucketService bitbucketService,
    IDeploymentAuthorizationService authorizationService,
    ILogger<DeploymentValidationService> logger) : IDeploymentValidationService
{
    public async Task<ValidationResult> ValidateAsync(
        string environment, string userId, IReadOnlyList<DeploymentItemRequest> items, CancellationToken cancellationToken)
    {
        logger.LogInformation("Validation started. Environment={Environment} RepositoryCount={Count}", environment, items.Count);

        if (!authorizationService.CanDeploy(userId, environment))
        {
            var denied = items
                .Select(item => Invalid(item, $"You do not have permission to deploy to {environment}."))
                .ToList();
            logger.LogInformation("Validation completed. Environment={Environment} Result=Denied (authorization)", environment);
            return new ValidationResult { IsValid = false, Items = denied };
        }

        var results = await Task.WhenAll(items.Select(item => ValidateItemAsync(item, cancellationToken)));
        var result = new ValidationResult { IsValid = results.All(r => r.IsValid), Items = results.ToList() };

        logger.LogInformation(
            "Validation completed. Environment={Environment} Result={Result}",
            environment, result.IsValid ? "Valid" : "Invalid");

        return result;
    }

    private async Task<ValidationItemResult> ValidateItemAsync(DeploymentItemRequest item, CancellationToken cancellationToken)
    {
        try
        {
            await bitbucketService.GetRepositoryAsync(item.Repository, cancellationToken);
        }
        catch (BitbucketApiException ex) when (ex.IsNotFound)
        {
            return Invalid(item, "Repository does not exist.");
        }

        try
        {
            await bitbucketService.GetBranchAsync(item.Repository, item.Branch, cancellationToken);
        }
        catch (BitbucketApiException ex) when (ex.IsNotFound)
        {
            return Invalid(item, "Branch does not exist.");
        }

        IReadOnlyList<Pipeline> pipelines;
        try
        {
            pipelines = await bitbucketService.GetPipelineDefinitionsAsync(item.Repository, item.Branch, cancellationToken);
        }
        catch (BitbucketApiException ex)
        {
            logger.LogWarning(ex, "Failed to load pipeline definitions during validation for Repository={Repository}", item.Repository);
            return Invalid(item, "Failed to load pipeline definitions from Bitbucket.");
        }

        var pipeline = pipelines.FirstOrDefault(p => p.Pattern == item.Pipeline);
        if (pipeline is null)
        {
            return Invalid(item, "Pipeline does not exist.");
        }

        if (pipeline.RequiresVariables && (item.Variables is null || item.Variables.Count == 0))
        {
            return Invalid(item, "This pipeline requires variables that were not supplied.");
        }

        return new ValidationItemResult { Repository = item.Repository, Branch = item.Branch, Pipeline = item.Pipeline, IsValid = true };
    }

    private static ValidationItemResult Invalid(DeploymentItemRequest item, string message) => new()
    {
        Repository = item.Repository,
        Branch = item.Branch,
        Pipeline = item.Pipeline,
        IsValid = false,
        ErrorMessage = message
    };
}

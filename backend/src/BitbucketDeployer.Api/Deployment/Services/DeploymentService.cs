using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.Services;

public class DeploymentService(
    IBitbucketService bitbucketService,
    IDeploymentValidationService validationService,
    IDeploymentStore store,
    ILogger<DeploymentService> logger) : IDeploymentService
{
    public Task<ValidationResult> ValidateAsync(CreateDeploymentRequest request, string userId, CancellationToken cancellationToken) =>
        validationService.ValidateAsync(request.Environment, userId, request.Repositories, cancellationToken);

    public async Task<Models.Deployment> CreateDeploymentAsync(CreateDeploymentRequest request, string userId, CancellationToken cancellationToken)
    {
        var validation = await validationService.ValidateAsync(request.Environment, userId, request.Repositories, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DeploymentValidationException(validation);
        }

        var deployment = new Models.Deployment
        {
            Id = store.GenerateNextId(),
            Environment = request.Environment,
            CreatedBy = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = DeploymentStatus.Running,
            Items = request.Repositories.Select(r => new DeploymentItem
            {
                Repository = r.Repository,
                Branch = r.Branch,
                Pipeline = r.Pipeline,
                Variables = r.Variables,
                Status = DeploymentStatus.Pending
            }).ToList()
        };
        store.Add(deployment);

        logger.LogInformation(
            "DeploymentId={DeploymentId} Environment={Environment} CreatedBy={User} Deployment requested for {Count} repositories",
            deployment.Id, deployment.Environment, userId, deployment.Items.Count);

        // Parallel triggering for MVP (section 23) — sequential/grouped orchestration can be added
        // later without changing this method's public contract.
        await Task.WhenAll(deployment.Items.Select(item => TriggerItemAsync(deployment.Id, item, cancellationToken)));

        deployment.Status = DeploymentStatusAggregator.Aggregate(deployment.Items.Select(i => i.Status).ToList());
        logger.LogInformation("DeploymentId={DeploymentId} Status={Status} Deployment completed triggering", deployment.Id, deployment.Status);

        return deployment;
    }

    public async Task<Models.Deployment?> GetDeploymentAsync(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = store.Get(deploymentId);
        if (deployment is null)
        {
            return null;
        }

        var nonTerminalItems = deployment.Items.Where(i => !i.Status.IsTerminal() && i.BitbucketPipelineUuid is not null).ToList();
        if (nonTerminalItems.Count > 0)
        {
            await Task.WhenAll(nonTerminalItems.Select(item => RefreshItemStatusAsync(deploymentId, item, cancellationToken)));
            deployment.Status = DeploymentStatusAggregator.Aggregate(deployment.Items.Select(i => i.Status).ToList());
        }

        return deployment;
    }

    public IReadOnlyList<Models.Deployment> GetHistory() => store.GetAll();

    public async Task<Models.Deployment> CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = store.Get(deploymentId) ?? throw new DeploymentNotFoundException(deploymentId);
        var cancellableItems = deployment.Items.Where(i => !i.Status.IsTerminal() && i.BitbucketPipelineUuid is not null).ToList();

        await Task.WhenAll(cancellableItems.Select(item => CancelItemAsync(deploymentId, item, cancellationToken)));

        deployment.Status = DeploymentStatusAggregator.Aggregate(deployment.Items.Select(i => i.Status).ToList());
        logger.LogInformation("DeploymentId={DeploymentId} Status={Status} Deployment cancelled by request", deploymentId, deployment.Status);
        return deployment;
    }

    public async Task<Models.Deployment> RetryDeploymentAsync(string deploymentId, CancellationToken cancellationToken)
    {
        var deployment = store.Get(deploymentId) ?? throw new DeploymentNotFoundException(deploymentId);
        var failedItems = deployment.Items.Where(i => i.Status == DeploymentStatus.Failed).ToList();

        if (failedItems.Count > 0)
        {
            await Task.WhenAll(failedItems.Select(item => TriggerItemAsync(deploymentId, item, cancellationToken)));
            deployment.Status = DeploymentStatusAggregator.Aggregate(deployment.Items.Select(i => i.Status).ToList());
            logger.LogInformation(
                "DeploymentId={DeploymentId} Status={Status} Retry requested for {Count} failed item(s)",
                deploymentId, deployment.Status, failedItems.Count);
        }

        return deployment;
    }

    private async Task TriggerItemAsync(string deploymentId, DeploymentItem item, CancellationToken cancellationToken)
    {
        item.Status = DeploymentStatus.Pending;
        item.ErrorMessage = null;

        try
        {
            logger.LogInformation(
                "DeploymentId={DeploymentId} Repository={Repository} Pipeline={Pipeline} Pipeline trigger requested",
                deploymentId, item.Repository, item.Pipeline);

            var result = await bitbucketService.TriggerPipelineAsync(item.Repository, item.Branch, item.Pipeline, item.Variables, cancellationToken);
            item.BitbucketPipelineUuid = result.PipelineUuid;
            item.Status = result.Status;

            logger.LogInformation(
                "DeploymentId={DeploymentId} Repository={Repository} Pipeline={Pipeline} Status={Status} Pipeline triggered",
                deploymentId, item.Repository, item.Pipeline, item.Status);
        }
        catch (BitbucketApiException ex)
        {
            item.Status = DeploymentStatus.Failed;
            item.ErrorMessage = ex.IsRateLimited
                ? "Bitbucket rate limit exceeded. Please retry shortly."
                : "Failed to trigger the pipeline.";

            logger.LogError(
                ex, "DeploymentId={DeploymentId} Repository={Repository} Pipeline={Pipeline} Pipeline trigger failed",
                deploymentId, item.Repository, item.Pipeline);
        }
    }

    private async Task RefreshItemStatusAsync(string deploymentId, DeploymentItem item, CancellationToken cancellationToken)
    {
        try
        {
            var status = await bitbucketService.GetPipelineStatusAsync(item.Repository, item.BitbucketPipelineUuid!, cancellationToken);
            if (status.Status != item.Status)
            {
                logger.LogInformation(
                    "DeploymentId={DeploymentId} Repository={Repository} Status={Status} Pipeline status changed from {OldStatus}",
                    deploymentId, item.Repository, status.Status, item.Status);
            }
            item.Status = status.Status;
        }
        catch (BitbucketApiException ex)
        {
            logger.LogWarning(
                ex, "DeploymentId={DeploymentId} Repository={Repository} Failed to refresh pipeline status", deploymentId, item.Repository);
        }
    }

    private async Task CancelItemAsync(string deploymentId, DeploymentItem item, CancellationToken cancellationToken)
    {
        try
        {
            await bitbucketService.CancelPipelineAsync(item.Repository, item.BitbucketPipelineUuid!, cancellationToken);
            item.Status = DeploymentStatus.Cancelled;
        }
        catch (BitbucketApiException ex)
        {
            logger.LogWarning(
                ex, "DeploymentId={DeploymentId} Repository={Repository} Failed to cancel pipeline", deploymentId, item.Repository);
        }
    }
}

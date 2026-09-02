using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class DeploymentItemResponse
{
    public required string Repository { get; init; }
    public required string Branch { get; init; }
    public required string Pipeline { get; init; }
    public string? BitbucketPipelineUuid { get; init; }
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }

    public static DeploymentItemResponse FromModel(DeploymentItem item) => new()
    {
        Repository = item.Repository,
        Branch = item.Branch,
        Pipeline = item.Pipeline,
        BitbucketPipelineUuid = item.BitbucketPipelineUuid,
        Status = item.Status.ToString(),
        ErrorMessage = item.ErrorMessage
    };
}

public class DeploymentResponse
{
    public required string Id { get; init; }
    public required string Environment { get; init; }
    public required string CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Status { get; init; }
    public required List<DeploymentItemResponse> Items { get; init; }

    public static DeploymentResponse FromModel(Models.Deployment deployment) => new()
    {
        Id = deployment.Id,
        Environment = deployment.Environment,
        CreatedBy = deployment.CreatedBy,
        CreatedAt = deployment.CreatedAt,
        Status = deployment.Status.ToString(),
        Items = deployment.Items.Select(DeploymentItemResponse.FromModel).ToList()
    };
}

public class DeploymentListItemResponse
{
    public required string Id { get; init; }
    public required string Environment { get; init; }
    public required string CreatedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public required string Status { get; init; }
    public int RepositoryCount { get; init; }

    public static DeploymentListItemResponse FromModel(Models.Deployment deployment) => new()
    {
        Id = deployment.Id,
        Environment = deployment.Environment,
        CreatedBy = deployment.CreatedBy,
        CreatedAt = deployment.CreatedAt,
        Status = deployment.Status.ToString(),
        RepositoryCount = deployment.Items.Count
    };
}

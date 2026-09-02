using BitbucketDeployer.Api.Deployment.Models;

namespace BitbucketDeployer.Api.Deployment.DTOs;

public class ValidationItemResultResponse
{
    public required string Repository { get; init; }
    public required string Branch { get; init; }
    public required string Pipeline { get; init; }
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationItemResultResponse FromModel(ValidationItemResult item) => new()
    {
        Repository = item.Repository,
        Branch = item.Branch,
        Pipeline = item.Pipeline,
        IsValid = item.IsValid,
        ErrorMessage = item.ErrorMessage
    };
}

public class ValidationResultResponse
{
    public bool IsValid { get; init; }
    public required List<ValidationItemResultResponse> Items { get; init; }

    public static ValidationResultResponse FromModel(ValidationResult result) => new()
    {
        IsValid = result.IsValid,
        Items = result.Items.Select(ValidationItemResultResponse.FromModel).ToList()
    };
}

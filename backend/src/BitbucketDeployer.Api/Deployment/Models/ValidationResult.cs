namespace BitbucketDeployer.Api.Deployment.Models;

public class ValidationItemResult
{
    public required string Repository { get; set; }
    public required string Branch { get; set; }
    public required string Pipeline { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public required List<ValidationItemResult> Items { get; set; }
}

namespace BitbucketDeployer.Api.Deployment.DTOs;

/// <summary>
/// Consistent error envelope for every non-2xx response. Message is always safe to show to the
/// user; raw Bitbucket bodies/exception details are logged server-side, never included here.
/// </summary>
public class ApiErrorResponse
{
    public required string Message { get; init; }
    public string? ErrorCode { get; init; }
    public string? TraceId { get; init; }
}

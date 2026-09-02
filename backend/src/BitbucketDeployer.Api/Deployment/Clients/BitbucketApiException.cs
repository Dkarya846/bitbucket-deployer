using System.Net;

namespace BitbucketDeployer.Api.Deployment.Clients;

/// <summary>
/// Raised whenever Bitbucket returns a non-success response. Carries enough detail for server-side
/// logging (StatusCode, RawBody) while callers translate it into a safe, user-facing message.
/// </summary>
public class BitbucketApiException(HttpStatusCode statusCode, string message, string? rawBody = null)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string? RawBody { get; } = rawBody;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsRateLimited => StatusCode == HttpStatusCode.TooManyRequests;
    public bool IsUnauthorized => StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
}

using Microsoft.AspNetCore.Diagnostics;
using BitbucketDeployer.Api.Auth.Services;
using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Services;

namespace BitbucketDeployer.Api.ErrorHandling;

/// <summary>
/// Single place that turns any unhandled exception into a consistent, safe response. Full
/// exception detail (including raw Bitbucket response bodies) is logged server-side only — never
/// returned to the client. See section 20/27 of the design brief.
/// </summary>
public class BitbucketApiExceptionHandler(ILogger<BitbucketApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception. TraceId={TraceId} Path={Path}",
            httpContext.TraceIdentifier, httpContext.Request.Path);

        // Deploy re-validation failures carry structured per-item detail the client needs to
        // render — they get their own response shape instead of the generic ApiErrorResponse.
        if (exception is DeploymentValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                ValidationResultResponse.FromModel(validationException.Result), cancellationToken);
            return true;
        }

        var (statusCode, message, errorCode) = Map(exception);
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse { Message = message, ErrorCode = errorCode, TraceId = httpContext.TraceIdentifier },
            cancellationToken);

        return true;
    }

    private static (int StatusCode, string Message, string ErrorCode) Map(Exception exception) => exception switch
    {
        NotAuthenticatedException =>
            (StatusCodes.Status401Unauthorized, exception.Message, "NOT_AUTHENTICATED"),

        BitbucketOAuthException =>
            (StatusCodes.Status502BadGateway, "Failed to complete Bitbucket login.", "BITBUCKET_OAUTH_ERROR"),

        DeploymentNotFoundException =>
            (StatusCodes.Status404NotFound, exception.Message, "DEPLOYMENT_NOT_FOUND"),

        BitbucketApiException { IsNotFound: true } =>
            (StatusCodes.Status404NotFound, "The requested Bitbucket resource was not found.", "BITBUCKET_NOT_FOUND"),

        BitbucketApiException { IsUnauthorized: true } =>
            (StatusCodes.Status502BadGateway, "Bitbucket rejected the configured credentials.", "BITBUCKET_AUTH_FAILED"),

        BitbucketApiException { IsRateLimited: true } =>
            (StatusCodes.Status429TooManyRequests, "Bitbucket rate limit exceeded. Please retry shortly.", "BITBUCKET_RATE_LIMITED"),

        BitbucketApiException =>
            (StatusCodes.Status502BadGateway, "Bitbucket returned an unexpected error.", "BITBUCKET_ERROR"),

        OperationCanceledException =>
            (StatusCodes.Status504GatewayTimeout, "The request to Bitbucket timed out.", "BITBUCKET_TIMEOUT"),

        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "INTERNAL_ERROR")
    };
}

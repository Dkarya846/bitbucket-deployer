namespace BitbucketDeployer.Api.Auth.Services;

/// <summary>Raised by BitbucketAuthHandler (via ICurrentSessionAccessor) when a request has no valid logged-in session.</summary>
public class NotAuthenticatedException() : Exception("Please log in with Bitbucket.");

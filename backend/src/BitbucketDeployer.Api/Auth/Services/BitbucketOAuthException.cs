namespace BitbucketDeployer.Api.Auth.Services;

/// <summary>Raised when the OAuth code/token exchange with Bitbucket itself fails.</summary>
public class BitbucketOAuthException(string message) : Exception(message);

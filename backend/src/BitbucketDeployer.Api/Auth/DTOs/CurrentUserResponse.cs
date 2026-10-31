namespace BitbucketDeployer.Api.Auth.DTOs;

public class CurrentUserResponse
{
    public bool IsLoggedIn { get; init; }
    public string? DisplayName { get; init; }
}

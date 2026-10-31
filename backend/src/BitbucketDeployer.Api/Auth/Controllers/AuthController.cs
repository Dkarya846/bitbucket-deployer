using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BitbucketDeployer.Api.Auth.Clients;
using BitbucketDeployer.Api.Auth.Configuration;
using BitbucketDeployer.Api.Auth.DTOs;
using BitbucketDeployer.Api.Auth.Services;

namespace BitbucketDeployer.Api.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IBitbucketOAuthClient oauthClient,
    IOAuthSessionStore sessionStore,
    ICurrentSessionAccessor currentSessionAccessor,
    IWebHostEnvironment environment,
    IOptions<BitbucketOAuthOptions> oauthOptions,
    IOptions<FrontendOptions> frontendOptions) : ControllerBase
{
    private const string StateCookieName = "bbd_oauth_state";
    private readonly BitbucketOAuthOptions _oauthOptions = oauthOptions.Value;
    private readonly FrontendOptions _frontendOptions = frontendOptions.Value;

    /// <summary>Starts the login flow: stashes a CSRF state value, then redirects to Bitbucket's consent screen.</summary>
    [HttpGet("login")]
    public IActionResult Login()
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        Response.Cookies.Append(StateCookieName, state, CookieOptionsFor(TimeSpan.FromMinutes(10)));

        var authorizeUrl = $"{_oauthOptions.AuthorizeUrl}?client_id={Uri.EscapeDataString(_oauthOptions.ClientId)}&response_type=code&state={Uri.EscapeDataString(state)}";
        return Redirect(authorizeUrl);
    }

    /// <summary>Bitbucket redirects here after the user approves (or denies) access.</summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var expectedState = Request.Cookies[StateCookieName];
        Response.Cookies.Delete(StateCookieName);

        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || state != expectedState)
        {
            return Redirect($"{_frontendOptions.BaseUrl}/login?error=1");
        }

        var tokenResponse = await oauthClient.ExchangeCodeAsync(code, cancellationToken);
        var user = await oauthClient.GetUserAsync(tokenResponse.AccessToken, cancellationToken);

        var session = await sessionStore.CreateAsync(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds),
            displayName: !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName! : user.Username ?? "Bitbucket user",
            username: user.Username ?? "unknown",
            cancellationToken);

        Response.Cookies.Append(CurrentSessionAccessor.CookieName, session.SessionId, CookieOptionsFor(TimeSpan.FromDays(30)));

        return Redirect($"{_frontendOptions.BaseUrl}/deploy");
    }

    /// <summary>Lets the frontend check login state on load without triggering a redirect.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var session = await currentSessionAccessor.GetCurrentSessionAsync(cancellationToken);
        return Ok(new CurrentUserResponse { IsLoggedIn = session is not null, DisplayName = session?.DisplayName });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(CurrentSessionAccessor.CookieName, out var sessionId))
        {
            await sessionStore.DeleteAsync(sessionId, cancellationToken);
            Response.Cookies.Delete(CurrentSessionAccessor.CookieName);
        }

        return NoContent();
    }

    private CookieOptions CookieOptionsFor(TimeSpan lifetime) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        // Secure requires HTTPS; the Angular dev server calls the API over plain HTTP by design
        // (see Program.cs's HTTPS-redirect note), so this only applies outside Development.
        Secure = !environment.IsDevelopment(),
        Expires = DateTimeOffset.UtcNow.Add(lifetime)
    };
}

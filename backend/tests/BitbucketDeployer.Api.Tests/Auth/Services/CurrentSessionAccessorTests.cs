using BitbucketDeployer.Api.Auth.Clients;
using BitbucketDeployer.Api.Auth.Clients.Wire;
using BitbucketDeployer.Api.Auth.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Auth.Services;

public class CurrentSessionAccessorTests
{
    private readonly Mock<IOAuthSessionStore> _sessionStore = new();
    private readonly Mock<IBitbucketOAuthClient> _oauthClient = new();

    private CurrentSessionAccessor CreateSut(string? cookieValue)
    {
        var httpContext = new DefaultHttpContext();
        if (cookieValue is not null)
        {
            httpContext.Request.Headers.Append("Cookie", $"{CurrentSessionAccessor.CookieName}={cookieValue}");
        }

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        return new CurrentSessionAccessor(accessor.Object, _sessionStore.Object, _oauthClient.Object, NullLogger<CurrentSessionAccessor>.Instance);
    }

    private static DecryptedSession ValidSession(DateTimeOffset? expiresAt = null) => new(
        "session-1", "access-token", "refresh-token", expiresAt ?? DateTimeOffset.UtcNow.AddHours(1), "Darshan Jain", "djain");

    [Fact]
    public async Task GetCurrentSessionAsync_NoCookie_ReturnsNull_AndNeverQueriesStore()
    {
        var sut = CreateSut(cookieValue: null);

        (await sut.GetCurrentSessionAsync(CancellationToken.None)).Should().BeNull();
        _sessionStore.Verify(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_CookiePresentButNoMatchingSession_ReturnsNull()
    {
        _sessionStore.Setup(s => s.GetAsync("session-1", It.IsAny<CancellationToken>())).ReturnsAsync((DecryptedSession?)null);
        var sut = CreateSut("session-1");

        (await sut.GetCurrentSessionAsync(CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentSessionAsync_NonExpiredSession_ReturnsItWithoutRefreshing()
    {
        var session = ValidSession();
        _sessionStore.Setup(s => s.GetAsync("session-1", It.IsAny<CancellationToken>())).ReturnsAsync(session);
        var sut = CreateSut("session-1");

        var result = await sut.GetCurrentSessionAsync(CancellationToken.None);

        result.Should().BeEquivalentTo(session);
        _oauthClient.Verify(c => c.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_ExpiringSoonSession_RefreshesAndPersistsNewTokens()
    {
        var expiringSession = ValidSession(DateTimeOffset.UtcNow.AddSeconds(30)); // inside the 2-minute refresh skew
        _sessionStore.Setup(s => s.GetAsync("session-1", It.IsAny<CancellationToken>())).ReturnsAsync(expiringSession);
        _oauthClient.Setup(c => c.RefreshAsync("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BitbucketOAuthTokenResponse { AccessToken = "new-access", RefreshToken = "new-refresh", ExpiresInSeconds = 7200 });

        var sut = CreateSut("session-1");
        var result = await sut.GetCurrentSessionAsync(CancellationToken.None);

        result!.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
        _sessionStore.Verify(
            s => s.UpdateTokensAsync("session-1", "new-access", "new-refresh", It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCurrentSessionAsync_RefreshFails_DeletesSessionAndReturnsNull()
    {
        var expiredSession = ValidSession(DateTimeOffset.UtcNow.AddMinutes(-5));
        _sessionStore.Setup(s => s.GetAsync("session-1", It.IsAny<CancellationToken>())).ReturnsAsync(expiredSession);
        _oauthClient.Setup(c => c.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketOAuthException("refresh token revoked"));

        var sut = CreateSut("session-1");
        var result = await sut.GetCurrentSessionAsync(CancellationToken.None);

        result.Should().BeNull();
        _sessionStore.Verify(s => s.DeleteAsync("session-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAccessTokenAsync_NoSession_ThrowsNotAuthenticatedException()
    {
        var sut = CreateSut(cookieValue: null);
        var act = () => sut.GetAccessTokenAsync(CancellationToken.None);
        await act.Should().ThrowAsync<NotAuthenticatedException>();
    }

    [Fact]
    public async Task GetDisplayNameAsync_NoSession_ThrowsNotAuthenticatedException()
    {
        var sut = CreateSut(cookieValue: null);
        var act = () => sut.GetDisplayNameAsync(CancellationToken.None);
        await act.Should().ThrowAsync<NotAuthenticatedException>();
    }

    [Fact]
    public async Task GetDisplayNameAsync_ValidSession_ReturnsDisplayName()
    {
        _sessionStore.Setup(s => s.GetAsync("session-1", It.IsAny<CancellationToken>())).ReturnsAsync(ValidSession());
        var sut = CreateSut("session-1");

        (await sut.GetDisplayNameAsync(CancellationToken.None)).Should().Be("Darshan Jain");
    }
}

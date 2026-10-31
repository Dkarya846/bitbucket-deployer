using BitbucketDeployer.Api.Auth.Services;
using BitbucketDeployer.Api.Deployment.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Auth.Services;

/// <summary>Uses a real (in-memory-mode) SQLite connection and a real data protector — the point is to verify the encrypt/decrypt round-trip actually works, not just that methods were called.</summary>
public class OAuthSessionStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeploymentDbContext _context;
    private readonly OAuthSessionStore _sut;

    public OAuthSessionStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _context = new DeploymentDbContext(new DbContextOptionsBuilder<DeploymentDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        _sut = new OAuthSessionStore(_context, new EphemeralDataProtectionProvider());
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateThenGet_RoundTripsDecryptedTokens()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(2);
        var created = await _sut.CreateAsync("access-123", "refresh-456", expiresAt, "Darshan Jain", "djain", CancellationToken.None);

        var fetched = await _sut.GetAsync(created.SessionId, CancellationToken.None);

        fetched.Should().NotBeNull();
        fetched!.AccessToken.Should().Be("access-123");
        fetched.RefreshToken.Should().Be("refresh-456");
        fetched.DisplayName.Should().Be("Darshan Jain");
        fetched.Username.Should().Be("djain");
        fetched.AccessTokenExpiresAt.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task TokensAreEncryptedAtRest_NotStoredAsPlaintext()
    {
        var created = await _sut.CreateAsync("super-secret-token", "super-secret-refresh", DateTimeOffset.UtcNow.AddHours(2), "A", "a", CancellationToken.None);

        var raw = await _context.OAuthSessions.FindAsync(created.SessionId);

        raw!.EncryptedAccessToken.Should().NotBe("super-secret-token");
        raw.EncryptedAccessToken.Should().NotContain("super-secret-token");
        raw.EncryptedRefreshToken.Should().NotContain("super-secret-refresh");
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        (await _sut.GetAsync("nope", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task UpdateTokensAsync_ReplacesTokensAndExpiry()
    {
        var created = await _sut.CreateAsync("old-access", "old-refresh", DateTimeOffset.UtcNow.AddMinutes(5), "A", "a", CancellationToken.None);
        var newExpiry = DateTimeOffset.UtcNow.AddHours(2);

        await _sut.UpdateTokensAsync(created.SessionId, "new-access", "new-refresh", newExpiry, CancellationToken.None);

        var fetched = await _sut.GetAsync(created.SessionId, CancellationToken.None);
        fetched!.AccessToken.Should().Be("new-access");
        fetched.RefreshToken.Should().Be("new-refresh");
        fetched.AccessTokenExpiresAt.Should().BeCloseTo(newExpiry, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_RemovesSession()
    {
        var created = await _sut.CreateAsync("a", "b", DateTimeOffset.UtcNow.AddHours(2), "A", "a", CancellationToken.None);

        await _sut.DeleteAsync(created.SessionId, CancellationToken.None);

        (await _sut.GetAsync(created.SessionId, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnknownId_DoesNotThrow()
    {
        var act = () => _sut.DeleteAsync("nope", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}

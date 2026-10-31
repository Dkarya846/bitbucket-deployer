using BitbucketDeployer.Api.Deployment.Models;
using BitbucketDeployer.Api.Deployment.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DeploymentRecord = BitbucketDeployer.Api.Deployment.Models.Deployment;

namespace BitbucketDeployer.Api.Tests.Deployment.Persistence;

/// <summary>
/// Uses a real (in-memory-mode) SQLite connection rather than mocks — the point of these tests is
/// to catch EF model-mapping mistakes (owned collection config, the Variables JSON converter,
/// enum-as-string) that a mock could never expose. A second DbContext sharing the same open
/// connection is used for verification reads, so assertions exercise actual SQLite storage rather
/// than EF's change-tracker identity map handing back the same in-memory object.
/// </summary>
public class SqliteDeploymentStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DeploymentDbContext _context;
    private readonly SqliteDeploymentStore _sut;

    public SqliteDeploymentStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _context = CreateContext();
        _context.Database.EnsureCreated();
        _sut = new SqliteDeploymentStore(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private DeploymentDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<DeploymentDbContext>().UseSqlite(_connection).Options);

    private static DeploymentRecord CreateDeployment(string id, DateTimeOffset? createdAt = null) => new()
    {
        Id = id,
        Environment = "uat",
        CreatedBy = "alice",
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        Status = DeploymentStatus.Running,
        Items =
        [
            new DeploymentItem
            {
                Repository = "ss-web",
                Branch = "uat",
                Pipeline = "deploy-uat",
                Status = DeploymentStatus.Queued,
                BitbucketPipelineUuid = "{uuid}",
                Variables = new Dictionary<string, string> { ["FOO"] = "bar" }
            }
        ]
    };

    [Fact]
    public void Add_ThenGetFromFreshContext_RoundTripsDeploymentAndItems()
    {
        _sut.Add(CreateDeployment("DEP-1"));

        using var freshContext = CreateContext();
        var reloaded = new SqliteDeploymentStore(freshContext).Get("DEP-1");

        reloaded.Should().NotBeNull();
        reloaded!.Environment.Should().Be("uat");
        reloaded.Status.Should().Be(DeploymentStatus.Running);
        reloaded.Items.Should().HaveCount(1);
        reloaded.Items[0].Repository.Should().Be("ss-web");
        reloaded.Items[0].Status.Should().Be(DeploymentStatus.Queued);
        reloaded.Items[0].BitbucketPipelineUuid.Should().Be("{uuid}");
        reloaded.Items[0].Variables.Should().ContainKey("FOO").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        _sut.Get("nope").Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_PersistsInPlaceMutations_VisibleFromAFreshContext()
    {
        _sut.Add(CreateDeployment("DEP-2"));

        var fetched = _sut.Get("DEP-2")!;
        fetched.Items[0].Status = DeploymentStatus.Successful;
        fetched.Status = DeploymentStatus.Successful;
        await _sut.SaveAsync(fetched, CancellationToken.None);

        using var freshContext = CreateContext();
        var reloaded = new SqliteDeploymentStore(freshContext).Get("DEP-2");

        reloaded!.Status.Should().Be(DeploymentStatus.Successful);
        reloaded.Items[0].Status.Should().Be(DeploymentStatus.Successful);
    }

    [Fact]
    public void GetAll_OrdersByCreatedAtDescending()
    {
        _sut.Add(CreateDeployment("DEP-A", DateTimeOffset.UtcNow.AddMinutes(-5)));
        _sut.Add(CreateDeployment("DEP-B", DateTimeOffset.UtcNow));

        _sut.GetAll().Select(d => d.Id).Should().ContainInOrder("DEP-B", "DEP-A");
    }

    [Fact]
    public void GenerateNextId_NoExistingRows_StartsAt001()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        _sut.GenerateNextId().Should().Be($"DEP-{today}-001");
    }

    [Fact]
    public void GenerateNextId_IncrementsPastExistingRowsForToday()
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        _sut.Add(CreateDeployment($"DEP-{today}-001"));
        _sut.Add(CreateDeployment($"DEP-{today}-002"));

        _sut.GenerateNextId().Should().Be($"DEP-{today}-003");
    }
}

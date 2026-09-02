using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Xunit;
using DeploymentRecord = BitbucketDeployer.Api.Deployment.Models.Deployment;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class InMemoryDeploymentStoreTests
{
    [Fact]
    public void GenerateNextId_IncrementsSequenceWithinSameDay()
    {
        var store = new InMemoryDeploymentStore();

        var first = store.GenerateNextId();
        var second = store.GenerateNextId();

        first.Should().MatchRegex(@"^DEP-\d{8}-001$");
        second.Should().MatchRegex(@"^DEP-\d{8}-002$");
    }

    [Fact]
    public void AddThenGet_ReturnsSameDeployment()
    {
        var store = new InMemoryDeploymentStore();
        var deployment = new DeploymentRecord
        {
            Id = "DEP-1", Environment = "uat", CreatedBy = "alice", CreatedAt = DateTimeOffset.UtcNow, Items = []
        };

        store.Add(deployment);

        store.Get("DEP-1").Should().BeSameAs(deployment);
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        new InMemoryDeploymentStore().Get("nope").Should().BeNull();
    }

    [Fact]
    public void GetAll_OrdersByCreatedAtDescending()
    {
        var store = new InMemoryDeploymentStore();
        var older = new DeploymentRecord { Id = "A", Environment = "uat", CreatedBy = "a", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5), Items = [] };
        var newer = new DeploymentRecord { Id = "B", Environment = "uat", CreatedBy = "a", CreatedAt = DateTimeOffset.UtcNow, Items = [] };
        store.Add(older);
        store.Add(newer);

        store.GetAll().Select(d => d.Id).Should().ContainInOrder("B", "A");
    }
}

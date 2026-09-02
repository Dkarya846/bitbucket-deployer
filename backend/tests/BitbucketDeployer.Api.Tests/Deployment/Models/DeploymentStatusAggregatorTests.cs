using BitbucketDeployer.Api.Deployment.Models;
using FluentAssertions;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Models;

public class DeploymentStatusAggregatorTests
{
    [Fact]
    public void Aggregate_Empty_ReturnsPending()
    {
        DeploymentStatusAggregator.Aggregate([]).Should().Be(DeploymentStatus.Pending);
    }

    [Fact]
    public void Aggregate_AnyFailed_ReturnsFailed_EvenIfOthersSucceeded()
    {
        var statuses = new[] { DeploymentStatus.Successful, DeploymentStatus.Failed, DeploymentStatus.Running };
        DeploymentStatusAggregator.Aggregate(statuses).Should().Be(DeploymentStatus.Failed);
    }

    [Fact]
    public void Aggregate_AllSuccessful_ReturnsSuccessful()
    {
        var statuses = new[] { DeploymentStatus.Successful, DeploymentStatus.Successful };
        DeploymentStatusAggregator.Aggregate(statuses).Should().Be(DeploymentStatus.Successful);
    }

    [Fact]
    public void Aggregate_MixOfRunningAndSuccessful_ReturnsRunning()
    {
        var statuses = new[] { DeploymentStatus.Successful, DeploymentStatus.Running };
        DeploymentStatusAggregator.Aggregate(statuses).Should().Be(DeploymentStatus.Running);
    }

    [Fact]
    public void Aggregate_AllPending_ReturnsPending()
    {
        var statuses = new[] { DeploymentStatus.Pending, DeploymentStatus.Pending };
        DeploymentStatusAggregator.Aggregate(statuses).Should().Be(DeploymentStatus.Pending);
    }

    [Fact]
    public void Aggregate_FailedTakesPrecedenceOverStopped()
    {
        var statuses = new[] { DeploymentStatus.Stopped, DeploymentStatus.Failed };
        DeploymentStatusAggregator.Aggregate(statuses).Should().Be(DeploymentStatus.Failed);
    }
}

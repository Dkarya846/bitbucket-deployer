using BitbucketDeployer.Api.Deployment.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Models;
using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class BitbucketStatusMapperTests
{
    [Fact]
    public void Map_NullState_ReturnsPending()
    {
        BitbucketStatusMapper.Map(null).Should().Be(DeploymentStatus.Pending);
    }

    [Fact]
    public void Map_Pending_ReturnsQueued()
    {
        var state = new BitbucketPipelineState { Name = "PENDING" };
        BitbucketStatusMapper.Map(state).Should().Be(DeploymentStatus.Queued);
    }

    [Theory]
    [InlineData("PENDING", DeploymentStatus.Queued)]
    [InlineData("RUNNING", DeploymentStatus.Running)]
    [InlineData("PAUSED", DeploymentStatus.Running)]
    [InlineData("HALTED", DeploymentStatus.Running)]
    public void Map_InProgress_MapsByStage(string stageName, DeploymentStatus expected)
    {
        var state = new BitbucketPipelineState
        {
            Name = "IN_PROGRESS",
            Stage = new BitbucketNamedValue { Name = stageName }
        };

        BitbucketStatusMapper.Map(state).Should().Be(expected);
    }

    [Theory]
    [InlineData("SUCCESSFUL", DeploymentStatus.Successful)]
    [InlineData("FAILED", DeploymentStatus.Failed)]
    [InlineData("ERROR", DeploymentStatus.Failed)]
    [InlineData("STOPPED", DeploymentStatus.Stopped)]
    public void Map_Completed_MapsByResult(string resultName, DeploymentStatus expected)
    {
        var state = new BitbucketPipelineState
        {
            Name = "COMPLETED",
            Result = new BitbucketNamedValue { Name = resultName }
        };

        BitbucketStatusMapper.Map(state).Should().Be(expected);
    }

    [Fact]
    public void Map_UnknownStateName_DefaultsToPending()
    {
        var state = new BitbucketPipelineState { Name = "SOMETHING_NEW" };
        BitbucketStatusMapper.Map(state).Should().Be(DeploymentStatus.Pending);
    }
}

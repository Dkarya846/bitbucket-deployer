using BitbucketDeployer.Api.Deployment.Configuration;
using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class DeploymentAuthorizationServiceTests
{
    [Theory]
    [InlineData("uat")]
    [InlineData("staging")]
    public void CanDeploy_NonProductionEnvironment_AlwaysAllowed(string environment)
    {
        var sut = new DeploymentAuthorizationService(Options.Create(new AuthorizationOptions()));

        sut.CanDeploy("anyone", environment).Should().BeTrue();
    }

    [Fact]
    public void CanDeploy_Production_DeniedWhenUserNotApproved()
    {
        var sut = new DeploymentAuthorizationService(Options.Create(new AuthorizationOptions { ProductionApprovedUsers = ["alice"] }));

        sut.CanDeploy("bob", "production").Should().BeFalse();
    }

    [Fact]
    public void CanDeploy_Production_AllowedWhenUserApproved_CaseInsensitive()
    {
        var sut = new DeploymentAuthorizationService(Options.Create(new AuthorizationOptions { ProductionApprovedUsers = ["Alice"] }));

        sut.CanDeploy("alice", "PRODUCTION").Should().BeTrue();
    }
}

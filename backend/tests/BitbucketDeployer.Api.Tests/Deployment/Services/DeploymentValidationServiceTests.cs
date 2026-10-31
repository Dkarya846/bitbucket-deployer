using System.Net;
using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.DTOs;
using BitbucketDeployer.Api.Deployment.Models;
using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class DeploymentValidationServiceTests
{
    private readonly Mock<IBitbucketService> _bitbucketService = new();
    private readonly Mock<IDeploymentAuthorizationService> _authorizationService = new();
    private readonly DeploymentValidationService _sut;

    public DeploymentValidationServiceTests()
    {
        _authorizationService.Setup(a => a.CanDeploy(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _sut = new DeploymentValidationService(_bitbucketService.Object, _authorizationService.Object, NullLogger<DeploymentValidationService>.Instance);

        _bitbucketService.Setup(s => s.GetRepositoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Repository { Slug = "ss-web", Name = "ss-web", MainBranch = "master" });
        _bitbucketService.Setup(s => s.GetBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Branch { Name = "uat" });
        _bitbucketService.Setup(s => s.GetPipelineDefinitionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Pipeline { Pattern = "deploy-uat", DisplayName = "Deploy Uat" }]);
    }

    private static DeploymentItemRequest Item(string repo = "ss-web", string branch = "uat", string pipeline = "deploy-uat", Dictionary<string, string>? variables = null) =>
        new() { Repository = repo, Branch = branch, Pipeline = pipeline, Variables = variables };

    [Fact]
    public async Task ValidateAsync_AuthorizationDenied_AllItemsInvalid_NoBitbucketCallsMade()
    {
        _authorizationService.Setup(a => a.CanDeploy("bob", "production")).Returns(false);

        var result = await _sut.ValidateAsync("production", "bob", [Item(), Item(repo: "ss-api")], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items.Should().AllSatisfy(i =>
        {
            i.IsValid.Should().BeFalse();
            i.ErrorMessage.Should().Contain("permission");
        });
        _bitbucketService.Verify(s => s.GetRepositoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_RepositoryNotFound_ItemInvalid()
    {
        _bitbucketService.Setup(s => s.GetRepositoryAsync("ss-missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.NotFound, "not found"));

        var result = await _sut.ValidateAsync("uat", "alice", [Item(repo: "ss-missing")], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items[0].ErrorMessage.Should().Be("Repository does not exist.");
    }

    [Fact]
    public async Task ValidateAsync_BranchNotFound_ItemInvalid()
    {
        _bitbucketService.Setup(s => s.GetBranchAsync("ss-web", "missing-branch", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.NotFound, "not found"));

        var result = await _sut.ValidateAsync("uat", "alice", [Item(branch: "missing-branch")], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items[0].ErrorMessage.Should().Be("Branch does not exist.");
    }

    [Fact]
    public async Task ValidateAsync_PipelineNotFound_ItemInvalid()
    {
        var result = await _sut.ValidateAsync("uat", "alice", [Item(pipeline: "does-not-exist")], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items[0].ErrorMessage.Should().Be("Pipeline does not exist.");
    }

    [Fact]
    public async Task ValidateAsync_PipelineRequiresVariable_NoneSupplied_ItemInvalid_NamesTheMissingVariable()
    {
        _bitbucketService.Setup(s => s.GetPipelineDefinitionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Pipeline { Pattern = "deploy-uat", DisplayName = "Deploy Uat", Variables = [new PipelineVariable { Name = "RELEASE_VERSION" }] }]);

        var result = await _sut.ValidateAsync("uat", "alice", [Item()], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items[0].ErrorMessage.Should().Contain("RELEASE_VERSION");
    }

    [Fact]
    public async Task ValidateAsync_PipelineRequiresVariable_SuppliedValueBlank_ItemInvalid()
    {
        _bitbucketService.Setup(s => s.GetPipelineDefinitionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Pipeline { Pattern = "deploy-uat", DisplayName = "Deploy Uat", Variables = [new PipelineVariable { Name = "RELEASE_VERSION" }] }]);

        var result = await _sut.ValidateAsync("uat", "alice", [Item(variables: new Dictionary<string, string> { ["RELEASE_VERSION"] = "   " })], CancellationToken.None);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_PipelineRequiresVariable_ValueSupplied_ItemValid()
    {
        _bitbucketService.Setup(s => s.GetPipelineDefinitionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Pipeline { Pattern = "deploy-uat", DisplayName = "Deploy Uat", Variables = [new PipelineVariable { Name = "RELEASE_VERSION" }] }]);

        var result = await _sut.ValidateAsync(
            "uat", "alice", [Item(variables: new Dictionary<string, string> { ["RELEASE_VERSION"] = "1.2.3" })], CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_VariableHasDefault_NotSupplied_StillValid()
    {
        _bitbucketService.Setup(s => s.GetPipelineDefinitionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Pipeline { Pattern = "deploy-uat", DisplayName = "Deploy Uat", Variables = [new PipelineVariable { Name = "CONFIRM", Default = "no" }] }]);

        var result = await _sut.ValidateAsync("uat", "alice", [Item()], CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AllValid_ReturnsValidTrue()
    {
        var result = await _sut.ValidateAsync("uat", "alice", [Item()], CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.Items[0].IsValid.Should().BeTrue();
        result.Items[0].ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_MultipleRepositories_ValidatesEachIndependently()
    {
        _bitbucketService.Setup(s => s.GetRepositoryAsync("ss-broken", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.NotFound, "not found"));

        var result = await _sut.ValidateAsync("uat", "alice", [Item(repo: "ss-web"), Item(repo: "ss-broken")], CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Items.Should().HaveCount(2);
        result.Items.Single(i => i.Repository == "ss-web").IsValid.Should().BeTrue();
        result.Items.Single(i => i.Repository == "ss-broken").IsValid.Should().BeFalse();
    }
}

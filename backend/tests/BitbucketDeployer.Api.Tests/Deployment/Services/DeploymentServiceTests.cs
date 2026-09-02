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

public class DeploymentServiceTests
{
    private readonly Mock<IBitbucketService> _bitbucketService = new();
    private readonly Mock<IDeploymentValidationService> _validationService = new();
    private readonly InMemoryDeploymentStore _store = new();
    private readonly DeploymentService _sut;

    public DeploymentServiceTests()
    {
        _sut = new DeploymentService(_bitbucketService.Object, _validationService.Object, _store, NullLogger<DeploymentService>.Instance);
    }

    private static CreateDeploymentRequest Request(params string[] repositories) => new()
    {
        Environment = "uat",
        Repositories = repositories.Select(r => new DeploymentItemRequest { Repository = r, Branch = "uat", Pipeline = "deploy-uat" }).ToList()
    };

    private void SetupValidation(bool isValid, IReadOnlyList<DeploymentItemRequest>? items = null)
    {
        _validationService
            .Setup(v => v.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<DeploymentItemRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                IsValid = isValid,
                Items = (items ?? []).Select(i => new ValidationItemResult
                {
                    Repository = i.Repository, Branch = i.Branch, Pipeline = i.Pipeline, IsValid = isValid
                }).ToList()
            });
    }

    [Fact]
    public async Task CreateDeploymentAsync_ValidationFails_ThrowsAndNeverTriggersPipeline()
    {
        var request = Request("ss-web");
        SetupValidation(isValid: false, request.Repositories);

        var act = () => _sut.CreateDeploymentAsync(request, "alice", CancellationToken.None);

        await act.Should().ThrowAsync<DeploymentValidationException>();
        _bitbucketService.Verify(
            s => s.TriggerPipelineAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateDeploymentAsync_MultipleRepositories_TriggersEachAndAggregatesStatus()
    {
        var request = Request("ss-web", "ss-api");
        SetupValidation(isValid: true, request.Repositories);

        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync(It.IsAny<string>(), "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{uuid}", BuildNumber = 1, Status = DeploymentStatus.Queued });

        var deployment = await _sut.CreateDeploymentAsync(request, "alice", CancellationToken.None);

        deployment.Id.Should().MatchRegex(@"^DEP-\d{8}-\d{3}$");
        deployment.Items.Should().HaveCount(2);
        deployment.Items.Should().AllSatisfy(i => i.Status.Should().Be(DeploymentStatus.Queued));
        deployment.Status.Should().Be(DeploymentStatus.Running);
        _store.Get(deployment.Id).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDeploymentAsync_OneRepositoryTriggerFails_OnlyThatItemFailsButDeploymentIsMarkedFailed()
    {
        var request = Request("ss-web", "ss-broken");
        SetupValidation(isValid: true, request.Repositories);

        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{ok}", BuildNumber = 1, Status = DeploymentStatus.Queued });
        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-broken", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.BadRequest, "boom"));

        var deployment = await _sut.CreateDeploymentAsync(request, "alice", CancellationToken.None);

        deployment.Items.Single(i => i.Repository == "ss-web").Status.Should().Be(DeploymentStatus.Queued);
        var broken = deployment.Items.Single(i => i.Repository == "ss-broken");
        broken.Status.Should().Be(DeploymentStatus.Failed);
        broken.ErrorMessage.Should().NotBeNullOrEmpty();
        deployment.Status.Should().Be(DeploymentStatus.Failed);
    }

    [Fact]
    public async Task GetDeploymentAsync_UnknownId_ReturnsNull()
    {
        (await _sut.GetDeploymentAsync("DEP-does-not-exist", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetDeploymentAsync_RefreshesNonTerminalItemsFromBitbucket()
    {
        var request = Request("ss-web");
        SetupValidation(isValid: true, request.Repositories);
        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{uuid}", BuildNumber = 1, Status = DeploymentStatus.Queued });
        var deployment = await _sut.CreateDeploymentAsync(request, "alice", CancellationToken.None);

        _bitbucketService
            .Setup(s => s.GetPipelineStatusAsync("ss-web", "{uuid}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{uuid}", BuildNumber = 1, Status = DeploymentStatus.Successful });

        var refreshed = await _sut.GetDeploymentAsync(deployment.Id, CancellationToken.None);

        refreshed!.Items[0].Status.Should().Be(DeploymentStatus.Successful);
        refreshed.Status.Should().Be(DeploymentStatus.Successful);
    }

    [Fact]
    public async Task CancelDeploymentAsync_UnknownId_ThrowsDeploymentNotFoundException()
    {
        var act = () => _sut.CancelDeploymentAsync("DEP-does-not-exist", CancellationToken.None);
        await act.Should().ThrowAsync<DeploymentNotFoundException>();
    }

    [Fact]
    public async Task RetryDeploymentAsync_OnlyRetriesFailedItems()
    {
        var request = Request("ss-web", "ss-broken");
        SetupValidation(isValid: true, request.Repositories);
        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{ok}", BuildNumber = 1, Status = DeploymentStatus.Successful });
        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-broken", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.BadRequest, "boom"));
        var deployment = await _sut.CreateDeploymentAsync(request, "alice", CancellationToken.None);

        _bitbucketService
            .Setup(s => s.TriggerPipelineAsync("ss-broken", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PipelineRunStatus { PipelineUuid = "{retry}", BuildNumber = 2, Status = DeploymentStatus.Queued });

        var retried = await _sut.RetryDeploymentAsync(deployment.Id, CancellationToken.None);

        retried.Items.Single(i => i.Repository == "ss-broken").Status.Should().Be(DeploymentStatus.Queued);
        retried.Items.Single(i => i.Repository == "ss-web").Status.Should().Be(DeploymentStatus.Successful);
        _bitbucketService.Verify(
            s => s.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

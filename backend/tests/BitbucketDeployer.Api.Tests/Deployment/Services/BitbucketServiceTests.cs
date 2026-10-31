using System.Net;
using BitbucketDeployer.Api.Deployment.Clients;
using BitbucketDeployer.Api.Deployment.Clients.Wire;
using BitbucketDeployer.Api.Deployment.Models;
using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class BitbucketServiceTests
{
    private readonly Mock<IBitbucketClient> _client = new();
    private readonly BitbucketService _sut;

    public BitbucketServiceTests()
    {
        _sut = new BitbucketService(_client.Object, NullLogger<BitbucketService>.Instance);
    }

    [Fact]
    public async Task GetRepositoriesAsync_MapsWireModelsAndPreservesPageToken()
    {
        _client.Setup(c => c.GetRepositoriesAsync("ss-", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BitbucketPagedResponse<BitbucketRepository>
            {
                Next = "https://api.bitbucket.org/2.0/repositories/ws?page=2",
                Values =
                [
                    new BitbucketRepository
                    {
                        Slug = "ss-web", Name = "ss-web",
                        MainBranch = new BitbucketMainBranch { Name = "master" },
                        Project = new BitbucketProject { Name = "Survey Sensum" }
                    }
                ]
            });

        var result = await _sut.GetRepositoriesAsync("ss-", null, CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Slug.Should().Be("ss-web");
        result.Items[0].MainBranch.Should().Be("master");
        result.Items[0].Project.Should().Be("Survey Sensum");
        result.NextPageToken.Should().Be("https://api.bitbucket.org/2.0/repositories/ws?page=2");
    }

    [Fact]
    public async Task GetRepositoryAsync_RepositoryNotFound_PropagatesBitbucketApiException()
    {
        _client.Setup(c => c.GetRepositoryAsync("does-not-exist", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.NotFound, "not found"));

        var act = () => _sut.GetRepositoryAsync("does-not-exist", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<BitbucketApiException>();
        ex.Which.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task GetPipelineDefinitionsAsync_NoYamlFile_ReturnsEmptyList()
    {
        _client.Setup(c => c.GetPipelinesYamlAsync("ss-web", "uat", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _sut.GetPipelineDefinitionsAsync("ss-web", "uat", CancellationToken.None);

        result.Should().BeEmpty();
        _client.Verify(c => c.GetPipelineRunsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPipelineDefinitionsAsync_ParsesYamlAndEnrichesWithLastRunStatus()
    {
        const string yaml = """
            pipelines:
              custom:
                deploy-uat:
                  - step:
                      script:
                        - ./deploy.sh uat
            """;

        _client.Setup(c => c.GetPipelinesYamlAsync("ss-web", "uat", It.IsAny<CancellationToken>())).ReturnsAsync(yaml);
        _client.Setup(c => c.GetPipelineRunsAsync("ss-web", null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BitbucketPagedResponse<BitbucketPipelineRun>
            {
                Values =
                [
                    new BitbucketPipelineRun
                    {
                        Uuid = "{run-1}",
                        CreatedOn = DateTimeOffset.UtcNow,
                        State = new BitbucketPipelineState { Name = "COMPLETED", Result = new BitbucketNamedValue { Name = "SUCCESSFUL" } },
                        Target = new BitbucketPipelineTarget { Selector = new BitbucketPipelineSelector { Type = "custom", Pattern = "deploy-uat" } }
                    }
                ]
            });

        var result = await _sut.GetPipelineDefinitionsAsync("ss-web", "uat", CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Pattern.Should().Be("deploy-uat");
        result[0].DisplayName.Should().Be("Deploy Uat");
        result[0].LastRunStatus.Should().Be(DeploymentStatus.Successful);
    }

    [Fact]
    public async Task GetPipelineDefinitionsAsync_MapsDeclaredVariables()
    {
        const string yaml = """
            pipelines:
              custom:
                deploy-with-approval:
                  - variables:
                      - name: RELEASE_VERSION
                      - name: CONFIRM
                        default: "no"
                        allowed-values:
                          - "yes"
                          - "no"
                  - step:
                      script:
                        - ./deploy.sh
            """;

        _client.Setup(c => c.GetPipelinesYamlAsync("ss-web", "uat", It.IsAny<CancellationToken>())).ReturnsAsync(yaml);
        _client.Setup(c => c.GetPipelineRunsAsync("ss-web", null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BitbucketPagedResponse<BitbucketPipelineRun> { Values = [] });

        var result = await _sut.GetPipelineDefinitionsAsync("ss-web", "uat", CancellationToken.None);

        var pipeline = result.Single(p => p.Pattern == "deploy-with-approval");
        pipeline.Variables.Should().HaveCount(2);
        pipeline.Variables.Single(v => v.Name == "RELEASE_VERSION").Default.Should().BeNull();
        var confirm = pipeline.Variables.Single(v => v.Name == "CONFIRM");
        confirm.Default.Should().Be("no");
        confirm.AllowedValues.Should().BeEquivalentTo("yes", "no");
    }

    [Fact]
    public async Task GetPipelineDefinitionsAsync_RunHistoryLookupFails_StillReturnsDefinitions()
    {
        const string yaml = """
            pipelines:
              custom:
                deploy-uat:
                  - step:
                      script:
                        - ./deploy.sh uat
            """;

        _client.Setup(c => c.GetPipelinesYamlAsync("ss-web", "uat", It.IsAny<CancellationToken>())).ReturnsAsync(yaml);
        _client.Setup(c => c.GetPipelineRunsAsync("ss-web", null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BitbucketApiException(HttpStatusCode.InternalServerError, "boom"));

        var result = await _sut.GetPipelineDefinitionsAsync("ss-web", "uat", CancellationToken.None);

        result.Should().ContainSingle();
        result[0].LastRunStatus.Should().BeNull();
    }

    [Fact]
    public async Task TriggerPipelineAsync_BuildsCorrectTargetAndMapsStatus()
    {
        BitbucketTriggerPipelineRequest? captured = null;
        _client.Setup(c => c.TriggerPipelineAsync("ss-web", It.IsAny<BitbucketTriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, BitbucketTriggerPipelineRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(new BitbucketPipelineRun
            {
                Uuid = "{new-run}",
                BuildNumber = 42,
                State = new BitbucketPipelineState { Name = "PENDING" }
            });

        var result = await _sut.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Target.RefName.Should().Be("uat");
        captured.Target.Selector.Type.Should().Be("custom");
        captured.Target.Selector.Pattern.Should().Be("deploy-uat");

        result.PipelineUuid.Should().Be("{new-run}");
        result.BuildNumber.Should().Be(42);
        result.Status.Should().Be(DeploymentStatus.Queued);
    }

    [Fact]
    public async Task TriggerPipelineAsync_MarksSensitiveLookingVariableNamesAsSecured()
    {
        BitbucketTriggerPipelineRequest? captured = null;
        _client.Setup(c => c.TriggerPipelineAsync("ss-web", It.IsAny<BitbucketTriggerPipelineRequest>(), It.IsAny<CancellationToken>()))
            .Callback<string, BitbucketTriggerPipelineRequest, CancellationToken>((_, req, _) => captured = req)
            .ReturnsAsync(new BitbucketPipelineRun { Uuid = "{run}", State = new BitbucketPipelineState { Name = "PENDING" } });

        var variables = new Dictionary<string, string>
        {
            ["RELEASE_VERSION"] = "1.2.3",
            ["DEPLOY_TOKEN"] = "abc123",
            ["DB_PASSWORD"] = "hunter2"
        };

        await _sut.TriggerPipelineAsync("ss-web", "uat", "deploy-uat", variables, CancellationToken.None);

        captured!.Variables.Should().NotBeNull();
        captured.Variables!.Single(v => v.Key == "RELEASE_VERSION").Secured.Should().BeFalse();
        captured.Variables!.Single(v => v.Key == "DEPLOY_TOKEN").Secured.Should().BeTrue();
        captured.Variables!.Single(v => v.Key == "DB_PASSWORD").Secured.Should().BeTrue();
    }
}

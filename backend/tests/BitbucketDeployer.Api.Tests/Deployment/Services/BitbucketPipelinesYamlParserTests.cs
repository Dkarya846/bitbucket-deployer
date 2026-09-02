using BitbucketDeployer.Api.Deployment.Services;
using FluentAssertions;
using Xunit;

namespace BitbucketDeployer.Api.Tests.Deployment.Services;

public class BitbucketPipelinesYamlParserTests
{
    [Fact]
    public void ParseCustomPipelines_ExtractsAllDefinitionNames()
    {
        const string yaml = """
            pipelines:
              custom:
                deploy-uat:
                  - step:
                      name: Deploy to UAT
                      script:
                        - ./deploy.sh uat
                deploy-production:
                  - variables:
                      - name: CONFIRM
                  - step:
                      name: Deploy to Production
                      script:
                        - ./deploy.sh production
                migration:
                  - step:
                      script:
                        - ./migrate.sh
            """;

        var result = BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml);

        result.Should().HaveCount(3);
        result.Select(p => p.Pattern).Should().BeEquivalentTo("deploy-uat", "deploy-production", "migration");
    }

    [Fact]
    public void ParseCustomPipelines_DetectsRequiredVariablesDeclaration()
    {
        const string yaml = """
            pipelines:
              custom:
                deploy-uat:
                  - step:
                      script:
                        - ./deploy.sh uat
                deploy-production:
                  - variables:
                      - name: CONFIRM
                  - step:
                      script:
                        - ./deploy.sh production
            """;

        var result = BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml);

        result.Single(p => p.Pattern == "deploy-uat").RequiresVariables.Should().BeFalse();
        result.Single(p => p.Pattern == "deploy-production").RequiresVariables.Should().BeTrue();
    }

    [Fact]
    public void ParseCustomPipelines_NoCustomSection_ReturnsEmpty()
    {
        const string yaml = """
            pipelines:
              default:
                - step:
                    script:
                      - echo hi
            """;

        BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml).Should().BeEmpty();
    }

    [Fact]
    public void ParseCustomPipelines_InvalidYaml_ReturnsEmptyRatherThanThrowing()
    {
        const string invalidYaml = "pipelines: [this: is: not: valid";

        var act = () => BitbucketPipelinesYamlParser.ParseCustomPipelines(invalidYaml);

        act.Should().NotThrow();
        act().Should().BeEmpty();
    }
}

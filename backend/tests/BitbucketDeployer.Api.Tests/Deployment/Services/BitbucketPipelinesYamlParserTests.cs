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
    public void ParseCustomPipelines_DetectsPipelinesWithAndWithoutVariables()
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

        result.Single(p => p.Pattern == "deploy-uat").Variables.Should().BeEmpty();
        result.Single(p => p.Pattern == "deploy-production").Variables.Should().ContainSingle(v => v.Name == "CONFIRM");
    }

    [Fact]
    public void ParseCustomPipelines_ExtractsDefaultAndAllowedValues()
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

        var result = BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml);
        var variables = result.Single(p => p.Pattern == "deploy-with-approval").Variables;

        variables.Should().HaveCount(2);

        var releaseVersion = variables.Single(v => v.Name == "RELEASE_VERSION");
        releaseVersion.Default.Should().BeNull();
        releaseVersion.AllowedValues.Should().BeNull();

        var confirm = variables.Single(v => v.Name == "CONFIRM");
        confirm.Default.Should().Be("no");
        confirm.AllowedValues.Should().BeEquivalentTo("yes", "no");
    }

    [Fact]
    public void ParseCustomPipelines_VariablesBlockInsideParallelStep_IsNotTreatedAsTopLevel()
    {
        // Bitbucket only recognizes `variables:` as a top-level sibling of `step`/`parallel` — a
        // `variables:` key nested inside a step's own map (unrealistic, but worth guarding) should
        // not be picked up.
        const string yaml = """
            pipelines:
              custom:
                deploy:
                  - parallel:
                      - step:
                          variables:
                            - name: NOT_A_REAL_DECLARATION
            """;

        var result = BitbucketPipelinesYamlParser.ParseCustomPipelines(yaml);

        result.Single(p => p.Pattern == "deploy").Variables.Should().BeEmpty();
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

using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace BitbucketDeployer.Api.Deployment.Services;

/// <summary>
/// Parses the `pipelines.custom` section of a bitbucket-pipelines.yml file. This is the
/// authoritative source of available custom pipeline *definitions* — Bitbucket's REST API has
/// no endpoint that lists them directly, only run history (see IBitbucketClient.GetPipelineRunsAsync).
/// </summary>
public static class BitbucketPipelinesYamlParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();

    public record ParsedPipelineDefinition(string Pattern, bool RequiresVariables);

    public static IReadOnlyList<ParsedPipelineDefinition> ParseCustomPipelines(string yamlContent)
    {
        object? root;
        try
        {
            root = Deserializer.Deserialize<object>(yamlContent);
        }
        catch (YamlException)
        {
            return [];
        }

        if (root is not IDictionary<object, object> rootMap) return [];
        if (!rootMap.TryGetValue("pipelines", out var pipelinesObj) || pipelinesObj is not IDictionary<object, object> pipelinesMap) return [];
        if (!pipelinesMap.TryGetValue("custom", out var customObj) || customObj is not IDictionary<object, object> customMap) return [];

        return customMap
            .Where(pair => pair.Key is string)
            .Select(pair => new ParsedPipelineDefinition((string)pair.Key, ContainsVariablesDeclaration(pair.Value)))
            .ToList();
    }

    private static bool ContainsVariablesDeclaration(object? node) => node switch
    {
        IList<object> list => list.Any(ContainsVariablesDeclaration),
        IDictionary<object, object> map => map.ContainsKey("variables"),
        _ => false
    };
}

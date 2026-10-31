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

    public record ParsedPipelineVariable(string Name, string? Default, IReadOnlyList<string>? AllowedValues);

    public record ParsedPipelineDefinition(string Pattern, IReadOnlyList<ParsedPipelineVariable> Variables);

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
            .Select(pair => new ParsedPipelineDefinition((string)pair.Key, ExtractVariables(pair.Value)))
            .ToList();
    }

    /// <summary>
    /// A custom pipeline's `variables:` block is always a sibling list item alongside its `step`/
    /// `parallel` entries, e.g. `[{variables: [...]}, {step: {...}}]` — never nested inside a step.
    /// </summary>
    private static IReadOnlyList<ParsedPipelineVariable> ExtractVariables(object? node) => node switch
    {
        IList<object> list => list.Select(ExtractVariables).FirstOrDefault(found => found.Count > 0) ?? [],
        IDictionary<object, object> map when map.TryGetValue("variables", out var variablesNode) => ParseVariableList(variablesNode),
        _ => []
    };

    private static IReadOnlyList<ParsedPipelineVariable> ParseVariableList(object? node)
    {
        if (node is not IList<object> list)
        {
            return [];
        }

        var result = new List<ParsedPipelineVariable>();
        foreach (var item in list)
        {
            if (item is not IDictionary<object, object> map) continue;
            if (!map.TryGetValue("name", out var nameObj) || nameObj is not string name) continue;

            var defaultValue = map.TryGetValue("default", out var d) ? d?.ToString() : null;
            IReadOnlyList<string>? allowedValues = map.TryGetValue("allowed-values", out var av) && av is IList<object> avList
                ? avList.Select(v => v?.ToString() ?? string.Empty).ToList()
                : null;

            result.Add(new ParsedPipelineVariable(name, defaultValue, allowedValues));
        }

        return result;
    }
}

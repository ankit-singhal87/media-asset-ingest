using System.Reflection;

namespace MediaIngest.Workflow.Orchestrator;

public sealed class WorkflowDefinitionCatalog
{
    private readonly IReadOnlyDictionary<string, WorkflowDefinition> definitions;

    private WorkflowDefinitionCatalog(IReadOnlyDictionary<string, WorkflowDefinition> definitions)
    {
        this.definitions = definitions;
    }

    public IReadOnlyCollection<WorkflowDefinition> Definitions => definitions.Values.ToArray();

    public static WorkflowDefinitionCatalog Discover(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            throw new WorkflowDefinitionCatalogException("At least one workflow definition assembly is required.");
        }

        return FromTypes(assemblies.SelectMany(assembly => assembly.DefinedTypes).ToArray());
    }

    public static WorkflowDefinitionCatalog FromTypes(params Type[] workflowDefinitionTypes)
    {
        var discovered = workflowDefinitionTypes
            .Select(type => CreateDefinition(type.GetTypeInfo()))
            .Where(definition => definition is not null)
            .Cast<WorkflowDefinition>()
            .ToArray();

        var duplicate = discovered
            .GroupBy(definition => definition.WorkflowName, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicate is not null)
        {
            throw new WorkflowDefinitionCatalogException($"Duplicate workflow definition id '{duplicate}'.");
        }

        return new WorkflowDefinitionCatalog(discovered.ToDictionary(
            definition => definition.WorkflowName,
            StringComparer.Ordinal));
    }

    public WorkflowDefinition? Find(string workflowName)
    {
        return definitions.GetValueOrDefault(workflowName);
    }

    public WorkflowDefinition GetRequired(string workflowName)
    {
        return Find(workflowName)
            ?? throw new WorkflowDefinitionCatalogException($"Workflow definition '{workflowName}' was not found.");
    }

    private static WorkflowDefinition? CreateDefinition(TypeInfo type)
    {
        var definitionAttribute = type.GetCustomAttribute<WorkflowDefinitionAttribute>();
        if (definitionAttribute is null)
        {
            return null;
        }

        RequireValue(definitionAttribute.WorkflowName, "Workflow definition id is required.");
        RequireValue(
            definitionAttribute.DisplayName,
            $"Workflow definition '{definitionAttribute.WorkflowName}' display name is required.");

        var nodeAttributes = type.GetCustomAttributes<WorkflowNodeAttribute>().ToArray();
        if (nodeAttributes.Length == 0)
        {
            throw new WorkflowDefinitionCatalogException(
                $"Workflow definition '{definitionAttribute.WorkflowName}' must declare at least one node.");
        }

        var nodes = nodeAttributes
            .Select(attribute =>
            {
                RequireValue(attribute.NodeId, $"Workflow definition '{definitionAttribute.WorkflowName}' contains a node without an id.");
                RequireValue(attribute.DisplayName, $"Workflow node '{attribute.NodeId}' display name is required.");

                return new WorkflowDefinitionNode(
                    attribute.NodeId,
                    attribute.DisplayName,
                    attribute.Kind,
                    attribute.ChildWorkflowName);
            })
            .ToArray();

        var duplicateNodeId = nodes
            .GroupBy(node => node.NodeId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateNodeId is not null)
        {
            throw new WorkflowDefinitionCatalogException(
                $"Workflow definition '{definitionAttribute.WorkflowName}' contains duplicate node id '{duplicateNodeId}'.");
        }

        var edges = type.GetCustomAttributes<WorkflowEdgeAttribute>()
            .Select(attribute =>
            {
                RequireValue(
                    attribute.SourceNodeId,
                    $"Workflow definition '{definitionAttribute.WorkflowName}' contains an edge without a source node id.");
                RequireValue(
                    attribute.TargetNodeId,
                    $"Workflow definition '{definitionAttribute.WorkflowName}' contains an edge without a target node id.");

                return new WorkflowDefinitionEdge(attribute.SourceNodeId, attribute.TargetNodeId);
            })
            .ToArray();

        var nodeIds = nodes.Select(node => node.NodeId).ToHashSet(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            if (!nodeIds.Contains(edge.SourceNodeId) || !nodeIds.Contains(edge.TargetNodeId))
            {
                throw new WorkflowDefinitionCatalogException(
                    $"Workflow definition '{definitionAttribute.WorkflowName}' contains an edge that references an unknown node.");
            }
        }

        return new WorkflowDefinition(
            definitionAttribute.WorkflowName,
            definitionAttribute.DisplayName,
            nodes,
            edges);
    }

    private static void RequireValue(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new WorkflowDefinitionCatalogException(message);
        }
    }
}

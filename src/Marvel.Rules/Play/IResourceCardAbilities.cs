using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text used while paying a cost.</summary>
public interface IResourceCardAbilities
{
    string ResourcesGeneratedBy(World world, Card source, Card? payingFor);
    IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player);
    IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player);
    string ResourceGeneratorName(World world, int player, int card);
    ResourceExecutionResult UseResource(World world, AbilityExecution entry, int player, int card);
}
#pragma warning restore CS1591

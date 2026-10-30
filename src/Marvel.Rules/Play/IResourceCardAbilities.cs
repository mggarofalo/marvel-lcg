using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

#pragma warning disable CS1591

namespace Marvel.Rules.Play;

/// <summary>Card text used while paying a cost.</summary>
public interface IResourceCardAbilities
{
    /// <summary>Whether printed card text permits this source to be discarded from hand for resources.</summary>
    /// <remarks>Eligibility is independent of generated quantity, including an empty contribution.</remarks>
    bool CanUseHandResources(World world, Card source);
    string ResourcesGeneratedBy(World world, Card source, Card? payingFor);
    /// <summary>Quote a hand contribution for the named card cost or its ability cost.</summary>
    ResourceSource QuoteHandResources(World world, Card source, Card? costOwner, ulong qualifyingComponents = 1);
    IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player);
    IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player);
    string ResourceGeneratorName(World world, int player, int card);
    ResourceExecutionResult UseResource(World world, AbilityExecution entry, int player, int card);
}
#pragma warning restore CS1591

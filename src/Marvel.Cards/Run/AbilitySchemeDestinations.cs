using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilitySchemeDestinations
{
    internal static Card? Resolve(World world, AbilityProgram program, Card enemy)
    {
        if (!program.SchemeDestinations.TryGetValue(enemy.FaceId, out var destination))
            return world.TheCardIn(DeckType.MainSchemesArea);
        if (destination != AbilitySchemeDestination.CorrespondingSideScheme)
            throw new RulesNotImplementedException($"'{enemy.FaceId}' has an unsupported scheme destination");

        // Wrecking Crew villains print "When [name] schemes, place the threat
        // on his side scheme instead of the main scheme." The engine chooses
        // immutable setup membership to identify that relation, never the active counter.
        var group = world.EncounterTopology?.Groups.SingleOrDefault(candidate => candidate.Stages.Contains(enemy));
        if (group is null || !ReferenceEquals(group.CurrentVillain, enemy)
            || group.Scheme.Area.Type != DeckType.SideSchemesArea)
            throw new RulesNotImplementedException(
                $"'{enemy.FaceId}' requires its living corresponding side scheme");
        return group.Scheme;
    }
}

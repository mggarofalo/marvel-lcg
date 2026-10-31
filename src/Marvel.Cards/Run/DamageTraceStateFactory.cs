using Marvel.Rules.State;
using Marvel.Rules.Play;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

internal static class DamageTraceStateFactory
{
    internal static DamageTraceState Initial(Card target, AbilityAdmissionScope cast)
    {
        int villain = EncounterRouting.Villain(cast.World, cast.Source)?.ObjectId ?? -1;
        return new(new Dictionary<int, long>(), target.Damage, [], new(), new(),
            new(), new(), TraceUnavailableMinions(cast), new(), new(), new(), new(), 0,
            cast.World.FirstPlayer, villain, 0, false)
        {
            EncounterStages = cast.World.EncounterTopology?.Groups.ToDictionary(
                group => group.Villain.ObjectId, group => group.CurrentVillain?.ObjectId ?? -1),
            EncounterOrder = cast.World.EncounterTopology is null ? null
                : cast.World.Areas.Where(area => area.Type == DeckType.VillainArea)
                    .SelectMany(area => area.Cards).Select(card => card.ObjectId).ToArray(),
        };
    }
}

using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;

namespace Marvel.Cards.Run;

/// <summary>Captures live initial facts for a read-only labelled-power projection.</summary>
internal static class AbilityPowerStateFactory
{
    internal static AbilityPowerState Initial(AbilityAdmissionScope cast)
    {
        var identity = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        int villain = EncounterRouting.Villain(cast.World, cast.Source)?.ObjectId ?? -1;
        return new AbilityPowerState(
            0, cast.World.FirstPlayer, identity.Damage,
            Statuses.Has(cast.World, identity, Statuses.Tough),
            new Dictionary<int, long>(), new Dictionary<int, bool>(),
            [], [], new Dictionary<int, PowerReadiness>(), TraceUnavailableMinions(cast),
            new Dictionary<int, long>(), new Dictionary<int, long>(), [], [], [],
            villain, 0, false)
        {
            EncounterOrder = cast.World.EncounterTopology is null ? null
                : cast.World.Areas.Where(area => area.Type == DeckType.VillainArea)
                    .SelectMany(area => area.Cards).Select(card => card.ObjectId).ToArray(),
            EncounterStages = cast.World.EncounterTopology?.Groups.ToDictionary(
                group => group.Villain.ObjectId, group => group.CurrentVillain?.ObjectId ?? -1),
        };
    }
}

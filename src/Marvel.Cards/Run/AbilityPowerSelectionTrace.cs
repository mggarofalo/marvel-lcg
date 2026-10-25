using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilitySelectorTrace;

namespace Marvel.Cards.Run;

/// <summary>Resolves card selections against a labelled power's projected board.</summary>
internal static class AbilityPowerSelectionTrace
{
    internal static Card? PowerFind(
        AbilityCardSelection value, AbilityAdmissionScope cast, AbilityPowerState reachability)
    {
        if (SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast))
        {
            if (reachability.EncounterStages is not null)
                return AbilitySingularTraceSelection.Find(value, new(reachability.CurrentVillain, cast,
                    reachability.Discarded, reachability.Traits, reachability.Modifiers, reachability.Engagement)
                { EncounterStages = reachability.EncounterStages, EncounterOrder = reachability.EncounterOrder,
                    SchemeThreat = reachability.SchemeThreat });
            return PowerEvery(value, cast, reachability).FirstOrDefault();
        }
        var found = Find(value, cast);
        int liveVillain = EncounterRouting.Villain(cast.World)?.ObjectId ?? -1;
        found = CurrentVillainCard(found, liveVillain, cast, reachability);
        return found is not null && !reachability.Discarded.Contains(found.ObjectId)
            ? found : null;
    }

    private static Card? CurrentVillainCard(
        Card? found, int liveVillain, AbilityAdmissionScope cast,
        AbilityPowerState reachability, bool sameCharacter = true)
    {
        if (found is not null && reachability.EncounterStages is not null)
            return sameCharacter
                ? AbilityPowerEncounterProjection.CurrentCharacter(found, cast, reachability.EncounterStages)
                : AbilityPowerEncounterProjection.Current(found, cast, reachability.EncounterStages);
        if (found?.ObjectId != liveVillain
            || reachability.CurrentVillain == liveVillain) return found;
        return reachability.Finished || reachability.CurrentVillain < 0
            ? null : cast.World.Cards[reachability.CurrentVillain];
    }

    internal static List<Card> PowerEvery(
        AbilityCardSelection value, AbilityAdmissionScope cast, AbilityPowerState reachability)
    {
        int liveVillain = EncounterRouting.Villain(cast.World)?.ObjectId ?? -1;
        bool dynamic = SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast);
        if (dynamic)
        {
            return DynamicPowerCards(value, cast, reachability, liveVillain);
        }
        return StablePowerCards(value, cast, reachability, liveVillain);
    }

    private static List<Card> DynamicPowerCards(
        AbilityCardSelection value, AbilityAdmissionScope cast,
        AbilityPowerState reachability, int liveVillain)
    {
        var candidates = TraceCandidateCards(value, cast);
        if (liveVillain >= 0 && PotentialVillainSelector(value, cast)
            && candidates.All(card => card.ObjectId != liveVillain))
        {
            candidates.Insert(0, cast.World.Cards[liveVillain]);
        }
        List<Card> selected =
        [
            .. AbilityEncounterCandidateOrder.Apply(candidates.Select(card => CurrentVillainCard(card, liveVillain, cast, reachability, sameCharacter: false))
                .Where(card => card is not null).Cast<Card>()
                .DistinctBy(card => card.ObjectId), cast.World,
                AbilityEncounterSchemeQueries.UsesGroupOrder(value) ? null : reachability.EncounterOrder)
                .Where(card => !reachability.Discarded.Contains(card.ObjectId)
                    && TraceSelectorMatches(
                        value, card, new(reachability.CurrentVillain, cast,
                        reachability.Discarded, reachability.Traits,
                            reachability.Modifiers, reachability.Engagement)
                            { EncounterStages = reachability.EncounterStages, EncounterOrder = reachability.EncounterOrder,
                                SchemeThreat = reachability.SchemeThreat })),
        ];
        return reachability.EncounterOrder is not null && UsesObjectIdOrder(value)
            ? [.. selected.OrderBy(card => card.ObjectId)] : selected;
    }

    private static List<Card> StablePowerCards(
        AbilityCardSelection value, AbilityAdmissionScope cast,
        AbilityPowerState reachability, int liveVillain)
    {
        var cards = new List<Card>();
        foreach (var found in Every(value, cast))
        {
            Card? card = CurrentVillainCard(found, liveVillain, cast, reachability);
            if (card is not null
                && !reachability.Discarded.Contains(card.ObjectId)
                && cards.All(existing => existing.ObjectId != card.ObjectId))
            {
                cards.Add(card);
            }
        }
        return cards;
    }

}

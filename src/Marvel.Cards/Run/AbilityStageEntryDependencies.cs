using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

/// <summary>Separates an opaque stage reveal from a stable upgrade departure.</summary>
internal static class AbilityStageEntryDependencies
{
    internal static bool PreservesReads(AbilityEffect node, AbilityPowerState state,
        AbilityAdmissionScope cast)
    {
        if (node is not AbilityEffect.CardAction
            { Instruction: AbilityCardInstruction.ReturnToHand,
              Selection: AbilityCardSelection.Titled } returned
            || state.EncounterStages is not null || state.CurrentVillain < 0)
            return false;
        var cards = Every(returned.Selection, cast).ToArray();
        if (cards.Length != 1 || !StableUpgrade(cards[0], state, cast)) return false;

        // Only public definitions are inspected. The search may find its named
        // side scheme or nothing; neither its presence nor shuffle is predicted.
        var stage = cast.World.Cards[state.CurrentVillain];
        if (!RevealPreserves(stage.FaceId, cast, [])) return false;
        if (!LiveObserversPreserve(cast)) return false;
        cast.World.Effects.PreflightConstantsEnding(cards[0]);
        return cast.World.Effects.Registered.Where(effect => effect.Kind == "health")
            .All(effect => effect.Scope.Length == 0);
    }

    private static bool StableUpgrade(Card card, AbilityPowerState state, AbilityAdmissionScope cast) =>
        card.Owner == cast.Player && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Upgrade
        && DeckTypes.IsInPlay(card.Area.Type) && !state.Discarded.Contains(card.ObjectId);
    private static bool LiveObserversPreserve(AbilityAdmissionScope cast)
    {
        foreach (var card in cast.World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)))
        foreach (var ability in AbilityProgramQueries.ActiveOn(cast.World, cast.Context.Program, card))
        {
            if (ability.Trigger.Timing == AbilityType.Constant
                && !StableConstant(ability.Effect)) return false;
            if (ability.Trigger.Event is Steps.CardRevealed or Steps.CardEntersPlay
                    or Steps.ThreatPlaced or Steps.ThreatWouldBePlaced
                && ability.Trigger.Timing is AbilityType.Interrupt or AbilityType.ForcedInterrupt
                    or AbilityType.Response or AbilityType.ForcedResponse) return false;
        }
        return true;
    }
    private static bool RevealPreserves(string face, AbilityAdmissionScope cast, HashSet<string> visiting)
    {
        if (!visiting.Add(face) || !cast.Context.Program.KnowsWhenRevealed(face)) return false;
        return cast.Context.Program.On(face).All(ability => ability.Trigger.Timing switch
        {
            AbilityType.WhenRevealed => ability.When is null && ability.Cost is null
                && RevealEffectPreserves(ability.Effect, cast, visiting),
            AbilityType.Constant => StableConstant(ability.Effect),
            _ => false,
        });
    }

    private static bool RevealEffectPreserves(AbilityEffect effect, AbilityAdmissionScope cast,
        HashSet<string> visiting) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.All(child =>
            RevealEffectPreserves(child, cast, new HashSet<string>(visiting))),
        AbilityEffect.GiveStatus { Cards: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.This } } => true,
        AbilityEffect.PlaceThreat { Schemes: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.This }, Amount: AbilityNumber.Constant or AbilityNumber.PerPlayer } => true,
        AbilityEffect.Search search => search.Areas.All(area => area is AbilitySearchArea.EncounterDeck
                or AbilitySearchArea.EncounterDiscardPile or AbilitySearchArea.ScenarioSetAside)
            && cast.World.Facts.Kind(search.Face) == CardKind.EncounterSideScheme
            && RevealPreserves(search.Face, cast, new HashSet<string>(visiting)),
        _ => false,
    };

    // Conditional health and recipient sets can change during reveal. Nonhealth
    // stat/trait grants cannot make a departure settle a new character defeat.
    private static bool StableConstant(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects.All(StableConstant),
        AbilityEffect.Conditional conditional => Harmless(conditional.Then) && Harmless(conditional.Else),
        AbilityEffect.GrantField { Until: null, Field: "health", Amount: AbilityNumber.Constant,
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This
                or AbilityCardBinding.You or AbilityCardBinding.YourHero
                or AbilityCardBinding.YourAlterEgo or AbilityCardBinding.AttachedTo } } => true,
        _ => Harmless(effect),
    };

    private static bool Harmless(AbilityEffect? effect) => effect switch
    {
        null => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(Harmless),
        AbilityEffect.Conditional conditional => Harmless(conditional.Then) && Harmless(conditional.Else),
        AbilityEffect.GrantTrait { Until: null } => true,
        AbilityEffect.GrantField { Until: null, Amount: AbilityNumber.Constant,
            Field: "attack" or "defense" or "thwart" or "hand_size" or "target_threat" } => true,
        _ => false,
    };
}





using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

using static Marvel.Cards.Run.AbilityAdmissionResolutionPreflight;
namespace Marvel.Cards.Run;

internal static class AbilityResolutionAdmission
{
    internal static bool CanPartiallyResolve(
        AbilityEffect node, AbilityAdmissionContext context) =>
        CanPartiallyResolve(node, new AbilityAdmissionScope(context, []));

    internal static AdmissionResolution ResolutionOf(
        AbilityEffect node, AbilityAdmissionContext context) =>
        ResolutionOf(node, new AbilityAdmissionScope(context, []));

    internal static AdmissionResolution EnsureDependentSupported(
        AbilityEffect node, AbilityAdmissionContext context,
        AbilityEffect effect, AbilityEffect dependent, AdmissionResolution required) =>
        EnsureDependentSupported(
            node, new AbilityAdmissionScope(context, []), effect, dependent, required);

    internal static void PreflightAnsweredOutcome(
        AbilityEffect node, AbilityAdmissionContext context) =>
        PreflightAnsweredOutcome(node, new AbilityAdmissionScope(context, []));

    internal static void PreflightResolutionBranches(
        AbilityEffect node, AbilityAdmissionContext context,
        bool allBranches = false) =>
        AbilityAdmissionResolutionPreflight.PreflightResolutionBranches(
            node, new AbilityAdmissionScope(context, []), allBranches);

    internal static bool ContainsNode(
        AbilityEffect node, string kind, AbilityAdmissionContext context) =>
        AbilityAdmissionResolutionPreflight.ContainsNode(
            node, kind, new AbilityAdmissionScope(context, []));

    internal static bool HasNestedEachPlayer(
        AbilityEffect node, AbilityAdmissionContext context, bool inside = false,
        bool stateMayChange = false, bool bindingMayChange = false,
        AbilityEffect? repeatedEffect = null) =>
        AbilityAdmissionResolutionPreflight.HasNestedEachPlayer(
            node, new AbilityAdmissionScope(context, []), inside,
            stateMayChange, bindingMayChange, repeatedEffect);

    /// <summary>Whether a player-card option can change the current state.</summary>
    internal static bool CanPartiallyResolve(AbilityEffect node, AbilityAdmissionScope cast)
    {
        return StructuralPartialResolution(node, cast)
            ?? CardStatePartialResolution(node, cast)
            ?? DamagePartialResolution(node, cast)
            ?? OtherPartialResolution(node, cast);
    }

    private static bool? StructuralPartialResolution(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "seq" or "and" => !OrderedEffects(node).Any()
                || OrderedEffects(node).Any(step => CanPartiallyResolve(step, cast)),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch && CanPartiallyResolve(branch, cast),
            "then" => ResolutionOf(EffectBody(node), cast)
                is not AdmissionResolution.None,
            "otherwise" => ResolutionOf(EffectBody(node), cast) switch
            {
                AdmissionResolution.None => CanPartiallyResolve(
                    EffectFollowing(node), cast),
                _ => true,
            },
            "defense" => CanPartiallyResolve(EffectBody(node), cast),
            "forEach" => ForEachCount(node, cast) > 0
                && CanPartiallyResolve(EffectBody(node), cast),
            "choose" => ((AbilityEffect.Choose)node).Options.Any(option => OptionIsLegal(option, cast)),
            "chooseCard" => LegalCardChoices(node, cast).Count > 0,
            "changeForm" => !AlreadyInForm(FormChangeOf(node, cast), cast),
            _ => null,
        };

    private static bool? CardStatePartialResolution(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "removeFromGame" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } card
                && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, card),
            "exhaust" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)?.Ready == true,
            "ready" => Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Any(card =>
                !card.Ready && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card)),
            "removeCounters" => CounterRemovalOf(node, cast) is var removal
                && Find(removal.Card, cast) is { } counterCard
                && CounterKeyForRemoval(
                    counterCard, removal.Counter, removal.Count) is not null,
            "advanceMainScheme" => CanAdvanceMainScheme(cast),
            "discardAtRandom" => Amount(EffectOf<AbilityEffect.DiscardAtRandom>(node, cast).Count, cast) > 0
                && Seats(EffectOf<AbilityEffect.DiscardAtRandom>(node, cast).Players, cast)
                    .Any(seat => cast.World.Seats[seat].Hand.Cards.Count > 0),
            "discardTop" => Amount(EffectOf<AbilityEffect.DiscardTop>(node, cast).Count, cast) > 0
                && DiscardTopHasCards((AbilityEffect.DiscardTop)node, cast),
            "heal" => Find(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast) is { Damage: > 0 }
                && Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast) > 0,
            _ => null,
        };

    private static bool? DamagePartialResolution(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "indirectDamage" => HasPartialResolutionTargets(node, cast)
                && Amount(EffectOf<AbilityEffect.IndirectDamage>(node, cast).Amount, cast) > 0,
            "dealDamage" => HasPartialResolutionTargets(node, cast)
                && Amount(EffectOf<AbilityEffect.Damage>(node, cast).Amount, cast) > 0,
            "dealAttackDamage" => HasPartialResolutionTargets(node, cast)
                && Amount(EffectOf<AbilityEffect.AttackDamage>(node, cast).Amount, cast) > 0,
            "placeThreat" => HasPartialResolutionTargets(node, cast)
                && Amount(EffectOf<AbilityEffect.PlaceThreat>(node, cast).Amount, cast) > 0,
            "removeThreat" => CanRemoveThreat(node, cast),
            "gainSurge" => EffectOf<AbilityEffect.GainSurge>(node, cast).Instances > 0,
            "draw" => CanDraw(node, cast),
            "drawToHandSize" => EffectOf<AbilityEffect.DrawToHandSize>(node, cast) is var handSize
                && cast.World.Seats[Seat(handSize.Player, cast)].Hand.Cards.Count
                < PhaseEnd.HandSize(
                    cast.World, cast.World.Seats[Seat(handSize.Player, cast)], cast.World.Facts),
            "drawToPrintedHandSize" => CanDrawToPrintedHandSize(node, cast),
            "createDrones" => CanCreateDrones(node, cast),
            "placeAccelerationToken" => HasPartialResolutionTargets(node, cast),
            _ => null,
        };

    private static bool OtherPartialResolution(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "preventThreat" => cast.Occurrence.Threat is { Remaining: > 0 }
                && Amount(EffectOf<AbilityEffect.PreventThreat>(node, cast).Amount, cast) > 0,
            "replaceThreatWithDamage" => cast.Occurrence.Threat is { Remaining: > 0 },
            "grantCharactersControlledBy" or "reduceNextCardCost" => true,

            // Target availability is the only state-dependent precondition
            // these currently expressible effects carry. Their own resolver
            // performs any further rule-specific work.
            "generate" or "soakDamage" or "preventDamage" or "cancelWhenRevealed"
                or "cancelOccurrence"
                or "dealEncounterCards" or "dealEncounterCard"
                or "revealTop" or "reveal" or "placeAtRandom"
                or "returnToHand" or "discardUntil" or "recoverDiscardedByResource"
                or "shuffleInto" or "search" or "giveStatus" or "declareDefender"
                or "attachTo"
                or "grantUntil" or "delayUntil" or "discard" or "enemyAttacks"
                or "enemySchemes" or "putIntoPlay" or "shuffle" =>
                    HasPartialResolutionTargets(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.OperationName()}' in an option whose partial "
                + "resolution is not implemented"),
        };

    /// <summary>How completely one effect can resolve on the current board.</summary>
    /// <remarks>
    /// <para>
    /// This is the distinction the printed dependency words need:
    /// <c>rr:then</c> requires <see cref="AdmissionResolution.Full"/>, while
    /// <c>rr:otherwise.1.2</c> permits its branch only for
    /// <see cref="AdmissionResolution.None"/>. A partial effect takes neither.
    /// </para>
    /// <para>
    /// It is deliberately a closed vocabulary. A node whose outcome has not
    /// been made explicit raises before the preceding effect mutates the board;
    /// guessing “full” would silently resolve dependent text that should not
    /// happen.
    /// </para>
    /// </remarks>
    internal static AdmissionResolution ResolutionOf(AbilityEffect node, AbilityAdmissionScope cast)
    {
        if (node.OperationName() is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect")
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.OperationName()}' before dependent text and it "
                + "suspends for a player choice");
        }

        return StructuralResolutionOf(node, cast) ?? LeafResolutionOf(node, cast);
    }

    private static AdmissionResolution? StructuralResolutionOf(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "seq" or "and" => CombinedOutcomes(
                OrderedEffects(node).Select(effect => ResolutionOf(effect, cast))),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch
                    ? ResolutionOf(branch, cast)
                    : AdmissionResolution.None,
            "forEach" when ForEachCount(node, cast) == 0 => AdmissionResolution.None,
            "changeForm" => AlreadyInForm(FormChangeOf(node, cast), cast)
                ? AdmissionResolution.None
                : AdmissionResolution.Full,
            _ => null,
        };

    private static AdmissionResolution LeafResolutionOf(
        AbilityEffect node, AbilityAdmissionScope cast) => node.OperationName() switch
        {
            "exhaust" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => card.Ready),
            "ready" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => !card.Ready
                    && AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card)),
            "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
                && Attack.CanDeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast))
                    ? AdmissionResolution.Full
                    : AdmissionResolution.None,
            "discard" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection is var discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? AdmissionResolution.Full
                    : AdmissionResolution.None,
            "draw" => CombinedOutcomes(Seats(EffectOf<AbilityEffect.Draw>(node, cast).Players, cast).Select(player =>
                ResolutionOfAmount(
                    cast.World.Seats[player].Deck.Cards.Count
                    + cast.World.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count,
                    EffectOf<AbilityEffect.Draw>(node, cast).Count))),
            "heal" => ResolutionOfAmount(
                Find(EffectOf<AbilityEffect.Heal>(node, cast).Card, cast)?.Damage ?? 0,
                Amount(EffectOf<AbilityEffect.Heal>(node, cast).Amount, cast)),
            "removeThreat" => ResolutionOfThreat(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.OperationName()}' before dependent text, whose "
                + "none/partial/full resolution is not implemented"),
        };

    internal static AdmissionResolution CombinedOutcomes(
        IEnumerable<AdmissionResolution> values)
    {
        var outcomes = values.ToList();
        if (outcomes.Count == 0 || outcomes.All(outcome => outcome == AdmissionResolution.None))
        {
            return AdmissionResolution.None;
        }

        return outcomes.All(outcome => outcome == AdmissionResolution.Full)
            ? AdmissionResolution.Full
            : AdmissionResolution.Partial;
    }

    internal static AdmissionResolution ResolutionOfCards(
        IReadOnlyList<Card> cards, Func<Card, bool> affected)
    {
        // `rr:target.4.1`: a multi-target effect does not resolve against
        // invalid elements. Completeness is therefore measured across the
        // targets the effect can affect, not every element named by "each".
        return cards.Any(affected)
            ? AdmissionResolution.Full
            : AdmissionResolution.None;
    }

    internal static AdmissionResolution ResolutionOfAmount(long available, long wanted)
    {
        if (available <= 0 || wanted <= 0)
        {
            return AdmissionResolution.None;
        }

        return available >= wanted
            ? AdmissionResolution.Full
            : AdmissionResolution.Partial;
    }

    internal static AdmissionResolution ResolutionOfThreat(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var schemes = Every(ThreatSelectionOf(node, cast), cast);
        long wanted = Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast);
        if (schemes.Count == 0 || wanted <= 0)
        {
            return AdmissionResolution.None;
        }

        var valid = schemes.Where(scheme =>
            scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && CanRemoveThreatFrom(node, cast, scheme));
        return CombinedOutcomes(valid.Select(scheme => ResolutionOfAmount(
            scheme.Tokens.GetValueOrDefault("k_threat"), wanted)));
    }

    internal static AdmissionResolution EnsureDependentSupported(
        AbilityEffect node,
        AbilityAdmissionScope cast,
        AbilityEffect effect,
        AbilityEffect dependent,
        AdmissionResolution required)
    {
        AbilityAdmissionResolutionPreflight.PreflightResolutionBranches(effect, cast);

        var outcome = ResolutionOf(effect, cast);
        bool stateMayChange = cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate;
        if ((outcome == required || stateMayChange)
            && AbilityAdmissionResolutionPreflight.ContainsNode(
                dependent, "placeThreat", cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.OperationName()}' before dependent text that "
                + "needs a nested continuation");
        }

        return outcome;
    }

    internal static void PreflightAnsweredOutcome(AbilityEffect node, AbilityAdmissionScope cast)
    {
        void PreflightEffect(AbilityEffect effect)
        {
            if (ActiveChoices(effect, cast).Any())
            {
                PreflightAnsweredOutcome(effect, cast);
            }
            else
            {
                _ = ResolutionOf(effect, cast);
            }
        }

        if (node.OperationName() == "choose")
        {
            foreach (var option in ((AbilityEffect.Choose)node).Options)
            {
                PreflightEffect(option);
            }
            return;
        }
        if (node.OperationName() == "chooseCard")
        {
            PreflightEffect(EffectBody(node));
            return;
        }
        var choices = ActiveChoices(node, cast).ToList();
        if (choices.Count > 0)
        {
            foreach (var choice in choices)
            {
                PreflightAnsweredOutcome(choice, cast);
            }
            return;
        }
        throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' uses '{node.OperationName()}' before dependent text, whose "
            + "answered resolution outcome is not implemented");
    }

}

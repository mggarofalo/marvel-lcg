using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    /// <summary>Whether a player-card option can change the current state.</summary>
    private static bool CanPartiallyResolve(AbilityEffect node, Cast cast)
    {
        return node.OperationName() switch
        {
            "seq" or "and" => !OrderedEffects(node).Any()
                || OrderedEffects(node).Any(step => CanPartiallyResolve(step, cast)),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch && CanPartiallyResolve(branch, cast),
            "then" => ResolutionOf(EffectBody(node), cast)
                is not ResolutionOutcome.None,
            "otherwise" => ResolutionOf(EffectBody(node), cast) switch
            {
                ResolutionOutcome.None => CanPartiallyResolve(
                    EffectFollowing(node), cast),
                _ => true,
            },
            "defense" => CanPartiallyResolve(EffectBody(node), cast),
            "forEach" => ForEachCount(node, cast) > 0
                && CanPartiallyResolve(EffectBody(node), cast),
            "choose" => ((AbilityEffect.Choose)node).Options.Any(option => OptionIsLegal(option, cast)),
            "chooseCard" => LegalCardChoices(node, cast).Count > 0,
            "changeForm" => !AlreadyInForm(FormChangeOf(node, cast), cast),
            "removeFromGame" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } card
                && CanRemoveByEffect(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast, card),
            "exhaust" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast)?.Ready == true,
            "ready" => Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast).Any(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source)),
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
    }

    /// <summary>How completely one effect can resolve on the current board.</summary>
    /// <remarks>
    /// <para>
    /// This is the distinction the printed dependency words need:
    /// <c>rr:then</c> requires <see cref="ResolutionOutcome.Full"/>, while
    /// <c>rr:otherwise.1.2</c> permits its branch only for
    /// <see cref="ResolutionOutcome.None"/>. A partial effect takes neither.
    /// </para>
    /// <para>
    /// It is deliberately a closed vocabulary. A node whose outcome has not
    /// been made explicit raises before the preceding effect mutates the board;
    /// guessing “full” would silently resolve dependent text that should not
    /// happen.
    /// </para>
    /// </remarks>
    private static ResolutionOutcome ResolutionOf(AbilityEffect node, Cast cast)
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

        return node.OperationName() switch
        {
            "seq" or "and" => CombinedOutcomes(
                OrderedEffects(node).Select(effect => ResolutionOf(effect, cast))),
            "if" => ConditionalBranch(node, Test(ConditionalOf(node, cast).Test, cast) ? "then" : "else")
                is { } branch
                    ? ResolutionOf(branch, cast)
                    : ResolutionOutcome.None,
            "forEach" when ForEachCount(node, cast) == 0 => ResolutionOutcome.None,
            "changeForm" => AlreadyInForm(FormChangeOf(node, cast), cast)
                ? ResolutionOutcome.None
                : ResolutionOutcome.Full,
            "exhaust" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => card.Ready),
            "ready" => ResolutionOfCards(
                Every(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast), card => !card.Ready
                    && cast.Abilities.CanReady(cast.World, card, cast.Source)),
            "declareDefender" => Find(EffectOf<AbilityEffect.CardAction>(node, cast).Selection, cast) is { } declared
                && Attack.CanDeclareByAbility(
                    cast.World, cast.World.Facts, declared,
                    ReplaceableDefenseDefender(cast))
                    ? ResolutionOutcome.Full
                    : ResolutionOutcome.None,
            "discard" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection is var discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? ResolutionOutcome.Full
                    : ResolutionOutcome.None,
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
    }

    private static ResolutionOutcome CombinedOutcomes(
        IEnumerable<ResolutionOutcome> values)
    {
        var outcomes = values.ToList();
        if (outcomes.Count == 0 || outcomes.All(outcome => outcome == ResolutionOutcome.None))
        {
            return ResolutionOutcome.None;
        }

        return outcomes.All(outcome => outcome == ResolutionOutcome.Full)
            ? ResolutionOutcome.Full
            : ResolutionOutcome.Partial;
    }

    private static ResolutionOutcome ResolutionOfCards(
        IReadOnlyList<Card> cards, Func<Card, bool> affected)
    {
        // `rr:target.4.1`: a multi-target effect does not resolve against
        // invalid elements. Completeness is therefore measured across the
        // targets the effect can affect, not every element named by "each".
        return cards.Any(affected)
            ? ResolutionOutcome.Full
            : ResolutionOutcome.None;
    }

    private static ResolutionOutcome ResolutionOfAmount(long available, long wanted)
    {
        if (available <= 0 || wanted <= 0)
        {
            return ResolutionOutcome.None;
        }

        return available >= wanted
            ? ResolutionOutcome.Full
            : ResolutionOutcome.Partial;
    }

    private static ResolutionOutcome ResolutionOfThreat(AbilityEffect node, Cast cast)
    {
        var schemes = Every(ThreatSelectionOf(node, cast), cast);
        long wanted = Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast);
        if (schemes.Count == 0 || wanted <= 0)
        {
            return ResolutionOutcome.None;
        }

        var valid = schemes.Where(scheme =>
            scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && CanRemoveThreatFrom(node, cast, scheme));
        return CombinedOutcomes(valid.Select(scheme => ResolutionOfAmount(
            scheme.Tokens.GetValueOrDefault("k_threat"), wanted)));
    }

    private static void ResolveDependent(
        AbilityEffect node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = EffectBody(node);
        var dependent = ContinuationChild(node, branch);
        if (ActiveChoices(effect, cast).Any())
        {
            PreflightAnsweredOutcome(effect, cast);
            PreflightContinuationBoundaries(dependent, cast);
            RunChild(effect, $"{node.OperationName()}:effect:Pending", cast);
            return;
        }
        var outcome = EnsureDependentSupported(node, cast, effect, dependent, required);

        // A supported predecessor classified as `None` changes no state. Some
        // low-level resolvers deliberately reject a missing target when used
        // alone; dependency words make that absence an expected outcome, so
        // do not turn an advertised `otherwise` fallback into an exception.
        if (outcome != ResolutionOutcome.None)
        {
            RunChild(effect, $"{node.OperationName()}:effect:{outcome}", cast);
        }
        if (outcome == required)
        {
            RunChild(dependent, $"{node.OperationName()}:{branch}", cast);
        }
    }

    private static ResolutionOutcome EnsureDependentSupported(
        AbilityEffect node,
        Cast cast,
        AbilityEffect effect,
        AbilityEffect dependent,
        ResolutionOutcome required)
    {
        PreflightResolutionBranches(effect, cast);

        var outcome = ResolutionOf(effect, cast);
        bool stateMayChange = cast.Reachability.PaymentMayMutate || cast.Reachability.PriorStepMayMutate;
        if ((outcome == required || stateMayChange)
            && ContainsNode(dependent, "placeThreat", cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.OperationName()}' before dependent text that "
                + "needs a nested continuation");
        }

        return outcome;
    }

    private static void PreflightAnsweredOutcome(AbilityEffect node, Cast cast)
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

    private static void PreflightResolutionBranches(
        AbilityEffect node, Cast cast, bool allBranches = false)
    {
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            var branches = allBranches || cast.Reachability.PriorStepMayMutate || PaymentCanChange(test)
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            foreach (var branch in branches)
            {
                PreflightResolutionBranches(branch, cast, allBranches);
            }
            return;
        }

        _ = ResolutionOf(node, cast);
    }

    private static bool PaymentCanChange(AbilityCondition test) => test switch
    {
        AbilityCondition.All all => all.Operands.Any(PaymentCanChange),
        AbilityCondition.Any any => any.Operands.Any(PaymentCanChange),
        AbilityCondition.Negated negated => PaymentCanChange(negated.Operand),

        // Paying an ability cannot change identity form. Other predicates may
        // read the chosen resources, the source's in-play status, or another
        // fact changed by an authored cost, so their branches are preflighted
        // conservatively.
        AbilityCondition.InForm => false,
        _ => true,
    };

    private static bool ContainsNode(AbilityEffect node, string kind, Cast cast) =>
        node.OperationName() == kind
        || !StableZeroForEach(node, cast)
            && ResolutionChildren(node).Any(child => ContainsNode(child, kind, cast));

    private static bool HasNestedEachPlayer(
        AbilityEffect node, Cast cast, bool inside = false, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityEffect? repeatedEffect = null)
    {
        if (inside && node.OperationName() == "eachPlayer")
        {
            return true;
        }
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (HasNestedEachPlayer(
                        EffectBody(node), cast, inside: true,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? EffectBody(node) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        bool within = inside || node.OperationName() == "eachPlayer";
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            HasNestedEachPlayer(
                child.Node, cast, within, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    private static bool ContainsUnsupportedPower(
        AbilityEffect node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityEffect? repeatedEffect = null)
    {
        if (node.OperationName() == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (ContainsUnsupportedPower(
                        EffectBody(node), cast,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? EffectBody(node) : repeatedEffect))
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                cast.RestorePlayer(original);
            }
        }
        if (node.OperationName() is "attack" or "thwart")
        {
            var prior = cast.CaptureChosen();
            try
            {
                var target = Find(EffectOf<AbilityEffect.Power>(node, cast).Target!, cast);
                bool targetWillBind = target is null;
                if (target is not null)
                {
                    cast.Choose(target);
                }
                if (SuspendsPowerEffect(
                    EffectBody(node), cast, stateMayChange,
                    bindingMayChange || targetWillBind))
                {
                    return true;
                }
            }
            finally
            {
                cast.RestoreChosen(prior);
            }
        }
        if (node.OperationName() == "thwartSchemes")
        {
            var power = ((AbilityEffect.ThwartGroup)node).Thwart;
            if (SuspendsPowerEffect(
                EffectBody(power), cast, stateMayChange, bindingMayChange))
            {
                return true;
            }
        }
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            ContainsUnsupportedPower(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    /// <summary>Executable children that can be reached after an ability is offered.</summary>
    private static IEnumerable<(
        AbilityEffect Node, bool StateMayChange, bool BindingMayChange)> GuardChildren(
        AbilityEffect node, Cast cast, bool stateMayChange, bool bindingMayChange,
        AbilityEffect? repeatedEffect)
    {
        if (node.OperationName() == "forEach")
        {
            bool countWillBind = bindingMayChange && HasUnboundPowerAmount(node, cast);
            long? count = countWillBind ? null : ForEachCount(node, cast);
            if (!countWillBind
                && (stateMayChange || bindingMayChange || cast.Reachability.PaymentMayMutate)
                && AmountMayChange(ForEachOf(node, cast).Count))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
            }
            if (count == 0)
            {
                return [];
            }
        }

        if (node.OperationName() == "seq")
        {
            return OrderedEffects(node).Select((child, index) =>
                (child, stateMayChange || index > 0, bindingMayChange));
        }
        if (node.OperationName() == "and")
        {
            var children = OrderedEffects(node).ToList();
            return children.Select(child =>
                (child, stateMayChange || children.Count > 1, bindingMayChange));
        }
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            bool canSwitch = stateMayChange
                || cast.Reachability.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test)
                || repeatedEffect is not null
                    && RepeatedEffectCanChange(test, repeatedEffect, cast);
            var branches = canSwitch
                ? ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                : ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(value =>
                (value, stateMayChange, bindingMayChange));
        }
        if (node.OperationName() is "then" or "otherwise")
        {
            var effect = EffectBody(node);
            var dependent = EffectFollowing(node);
            var required = node.OperationName() == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            bool answered = ActiveChoices(effect, cast).Any();
            bool dependentCanRun = stateMayChange
                || cast.Reachability.PaymentMayMutate
                || bindingMayChange
                || answered
                || ResolutionOf(effect, cast) == required;
            bool predecessorMayMutate = stateMayChange
                || cast.Reachability.PaymentMayMutate
                || node.OperationName() == "then"
                || answered;
            return dependentCanRun
                ? [
                    (effect, stateMayChange, bindingMayChange),
                    (dependent, predecessorMayMutate, bindingMayChange),
                ]
                : [(effect, stateMayChange, bindingMayChange)];
        }
        if (node.OperationName() is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return ContinuationChildren(node).Select(child =>
                (child, stateMayChange, true));
        }
        if (node.OperationName() is "afterActivation" or "delayUntil" or "defense"
            or "payOrEffect" or "payOrExhaust")
        {
            return ContinuationChildren(node).Select(child =>
                (child, true, bindingMayChange));
        }
        return ContinuationChildren(node).Select(child =>
            (child, stateMayChange, bindingMayChange));
    }

}

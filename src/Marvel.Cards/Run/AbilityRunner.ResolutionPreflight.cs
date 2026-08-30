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
    private static bool CanPartiallyResolve(AbilityNode node, Cast cast)
    {
        return node.Kind switch
        {
            "seq" or "and" => !Nodes(node.Argument).Any()
                || Nodes(node.Argument).Any(step => CanPartiallyResolve(step, cast)),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch && CanPartiallyResolve(Tree(branch), cast),
            "then" => ResolutionOf(Tree(node.Require("effect")), cast)
                is not ResolutionOutcome.None,
            "otherwise" => ResolutionOf(Tree(node.Require("effect")), cast) switch
            {
                ResolutionOutcome.None => CanPartiallyResolve(
                    Tree(node.Require("otherwise")), cast),
                _ => true,
            },
            "defense" => CanPartiallyResolve(Tree(node.Require("effect")), cast),
            "forEach" => Amount(node.Require("count"), cast) > 0
                && CanPartiallyResolve(Tree(node.Require("effect")), cast),
            "choose" => Nodes(node.Require("options")).Any(option => OptionIsLegal(option, cast)),
            "chooseCard" => LegalCardChoices(node, cast).Count > 0,
            "changeForm" => !Forms.In(
                cast.World,
                cast.World.Seats[Seat(node.Require("player"), cast)],
                cast.World.Facts,
                Word(node.Require("to"))),
            "removeFromGame" => Find(node.Argument, cast) is { } card
                && CanRemoveByEffect(node.Argument, cast, card),
            "exhaust" => Find(node.Argument, cast)?.Ready == true,
            "ready" => Every(node.Argument, cast).Any(card =>
                !card.Ready && cast.Abilities.CanReady(cast.World, card, cast.Source)),
            "removeCounters" =>
                CounterKeyForRemoval(cast.Source, Word(node.Argument)) is not null,
            "advanceMainScheme" => CanAdvanceMainScheme(node, cast),
            "discardAtRandom" => Amount(node.Require("count"), cast) > 0
                && Seats(node.Require("player"), cast)
                    .Any(seat => cast.World.Seats[seat].Hand.Cards.Count > 0),
            "discardTop" => Amount(node.Require("count"), cast) > 0
                && Area(Word(node.Require("from")), cast).Cards.Count > 0,
            "heal" => Find(node.Require("card"), cast) is { Damage: > 0 }
                && Amount(node.Require("amount"), cast) > 0,
            "indirectDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "dealDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "dealAttackDamage" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "placeThreat" => HasRequiredTargets(node, cast)
                && Amount(node.Require("amount"), cast) > 0,
            "removeThreat" => CanRemoveThreat(node, cast),
            "gainSurge" => Number(node.Argument) > 0,
            "draw" => CanDraw(node, cast),
            "drawToHandSize" => cast.World.Seats[Seat(node.Argument, cast)].Hand.Cards.Count
                < PhaseEnd.HandSize(
                    cast.World, cast.World.Seats[Seat(node.Argument, cast)], cast.World.Facts),
            "drawToPrintedHandSize" => CanDrawToPrintedHandSize(node, cast),
            "createDrones" => CanCreateDrones(node, cast),
            "placeAccelerationToken" => HasRequiredTargets(node, cast),
            "preventThreat" => cast.Occurrence.Threat is { Remaining: > 0 }
                && Amount(node.Argument, cast) > 0,
            "replaceThreatWithDamage" => cast.Occurrence.Threat is { Remaining: > 0 },
            "grantCharactersControlledBy" or "reduceNextCardCost" => true,

            // Target availability is the only state-dependent precondition
            // these currently expressible effects carry. Their own resolver
            // performs any further rule-specific work.
            "generate" or "soakDamage" or "preventDamage" or "cancelWhenRevealed"
                or "dealEncounterCards" or "dealEncounterCard"
                or "revealTop" or "reveal" or "placeAtRandom"
                or "returnToHand" or "discardUntil" or "recoverDiscardedByResource"
                or "shuffleInto" or "search" or "giveStatus" or "attachTo"
                or "grantUntil" or "delayUntil" or "discard" or "enemyAttacks"
                or "enemySchemes" or "putIntoPlay" or "shuffle" =>
                    HasRequiredTargets(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' in an option whose partial "
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
    private static ResolutionOutcome ResolutionOf(AbilityNode node, Cast cast)
    {
        if (node.Kind is "choose" or "chooseCard" or "indirectDamage"
            or "resolveSpecials" or "payOrExhaust" or "chooseTopForHand"
            or "chooseDiscardToShuffle" or "thwartDifferentSchemes" or "makeTheCall"
            or "legalPractice" or "payOrEffect")
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text and it "
                + "suspends for a player choice");
        }

        return node.Kind switch
        {
            "seq" or "and" => CombinedOutcomes(
                Nodes(node.Argument).Select(effect => ResolutionOf(effect, cast))),
            "if" => node.Field(Test(Tree(node.Require("test")), cast) ? "then" : "else")
                is { } branch
                    ? ResolutionOf(Tree(branch), cast)
                    : ResolutionOutcome.None,
            "forEach" when ForEachCount(node, cast) == 0 => ResolutionOutcome.None,
            "changeForm" => Forms.In(
                    cast.World,
                    cast.World.Seats[Seat(node.Require("player"), cast)],
                    cast.World.Facts,
                    Word(node.Require("to")))
                ? ResolutionOutcome.None
                : ResolutionOutcome.Full,
            "exhaust" => ResolutionOfCards(
                Every(node.Argument, cast), card => card.Ready),
            "ready" => ResolutionOfCards(
                Every(node.Argument, cast), card => !card.Ready
                    && cast.Abilities.CanReady(cast.World, card, cast.Source)),
            "discard" => (node.Field("card") ?? node.Argument) is { } discardTarget
                && Find(discardTarget, cast) is { } discarded
                && CanRemoveByEffect(discardTarget, cast, discarded)
                    ? ResolutionOutcome.Full
                    : ResolutionOutcome.None,
            "draw" => CombinedOutcomes(Seats(node.Require("player"), cast).Select(player =>
                ResolutionOfAmount(
                    cast.World.Seats[player].Deck.Cards.Count
                    + cast.World.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count,
                    Number(node.Require("count"))))),
            "heal" => ResolutionOfAmount(
                Find(node.Require("card"), cast)?.Damage ?? 0,
                Amount(node.Require("amount"), cast)),
            "removeThreat" => ResolutionOfThreat(node, cast),
            _ => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text, whose "
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

    private static ResolutionOutcome ResolutionOfThreat(AbilityNode node, Cast cast)
    {
        var schemes = Every(node.Require("scheme"), cast);
        long wanted = Amount(node.Require("amount"), cast);
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
        AbilityNode node, Cast cast, ResolutionOutcome required, string branch)
    {
        var effect = Tree(node.Require("effect"));
        var dependent = Tree(node.Require(branch));
        if (ActiveChoices(effect, cast).Any())
        {
            PreflightAnsweredOutcome(effect, cast);
            PreflightContinuationBoundaries(dependent, cast);
            RunChild(effect, $"{node.Kind}:effect:Pending", cast);
            return;
        }
        var outcome = EnsureDependentSupported(node, cast, effect, dependent, required);

        // A supported predecessor classified as `None` changes no state. Some
        // low-level resolvers deliberately reject a missing target when used
        // alone; dependency words make that absence an expected outcome, so
        // do not turn an advertised `otherwise` fallback into an exception.
        if (outcome != ResolutionOutcome.None)
        {
            RunChild(effect, $"{node.Kind}:effect:{outcome}", cast);
        }
        if (outcome == required)
        {
            RunChild(dependent, $"{node.Kind}:{branch}", cast);
        }
    }

    private static ResolutionOutcome EnsureDependentSupported(
        AbilityNode node,
        Cast cast,
        AbilityNode effect,
        AbilityNode dependent,
        ResolutionOutcome required)
    {
        PreflightResolutionBranches(effect, cast);

        var outcome = ResolutionOf(effect, cast);
        bool stateMayChange = cast.PaymentMayMutate || cast.PriorStepMayMutate;
        if ((outcome == required || stateMayChange)
            && ContainsNode(dependent, "placeThreat", cast))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text that "
                + "needs a nested continuation");
        }

        return outcome;
    }

    private static void PreflightAnsweredOutcome(AbilityNode node, Cast cast)
    {
        void PreflightEffect(AbilityNode effect)
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

        if (node.Kind == "choose")
        {
            foreach (var option in Nodes(node.Require("options")))
            {
                PreflightEffect(option);
            }
            return;
        }
        if (node.Kind == "chooseCard")
        {
            PreflightEffect(Tree(node.Require("effect")));
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
            $"'{cast.Source.FaceId}' uses '{node.Kind}' before dependent text, whose "
            + "answered resolution outcome is not implemented");
    }

    private static void PreflightResolutionBranches(
        AbilityNode node, Cast cast, bool allBranches = false)
    {
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            var branches = allBranches || cast.PriorStepMayMutate || PaymentCanChange(test)
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            foreach (var branch in branches)
            {
                PreflightResolutionBranches(Tree(branch!), cast, allBranches);
            }
            return;
        }

        _ = ResolutionOf(node, cast);
    }

    private static bool PaymentCanChange(AbilityNode test) => test.Kind switch
    {
        "and" or "or" => Nodes(test.Argument).Any(PaymentCanChange),
        "not" => PaymentCanChange(Tree(test.Argument)),

        // Paying an ability cannot change identity form. Other predicates may
        // read the chosen resources, the source's in-play status, or another
        // fact changed by an authored cost, so their branches are preflighted
        // conservatively.
        "inForm" => false,
        _ => true,
    };

    private static bool ContainsNode(AbilityNode node, string kind, Cast cast) =>
        node.Kind == kind
        || !StableZeroForEach(node, cast)
            && StructuralChildren(node).Any(child => ContainsNode(child, kind, cast));

    private static bool HasNestedEachPlayer(
        AbilityNode node, Cast cast, bool inside = false, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityNode? repeatedEffect = null)
    {
        if (inside && node.Kind == "eachPlayer")
        {
            return true;
        }
        if (node.Kind == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (HasNestedEachPlayer(
                        Tree(node.Require("effect")), cast, inside: true,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? Tree(node.Require("effect")) : repeatedEffect))
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
        bool within = inside || node.Kind == "eachPlayer";
        return GuardChildren(
            node, cast, stateMayChange, bindingMayChange, repeatedEffect).Any(child =>
            HasNestedEachPlayer(
                child.Node, cast, within, child.StateMayChange,
                child.BindingMayChange, repeatedEffect));
    }

    private static bool ContainsUnsupportedPower(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false, AbilityNode? repeatedEffect = null)
    {
        if (node.Kind == "eachPlayer")
        {
            int original = cast.Player;
            try
            {
                var players = cast.World.PlayerOrder.ToList();
                foreach (int player in players)
                {
                    cast.RestorePlayer(player);
                    if (ContainsUnsupportedPower(
                        Tree(node.Require("effect")), cast,
                        stateMayChange, bindingMayChange,
                        players.Count > 1 ? Tree(node.Require("effect")) : repeatedEffect))
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
        if (node.Kind is "attack" or "thwart")
        {
            var prior = cast.CaptureChosen();
            try
            {
                var target = Find(node.Require("target"), cast);
                bool targetWillBind = target is null;
                if (target is not null)
                {
                    cast.Choose(target);
                }
                if (SuspendsPowerEffect(
                    Tree(node.Require("effect")), cast, stateMayChange,
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
        if (node.Kind == "thwartSchemes")
        {
            var power = Tree(node.Require("power"));
            if (SuspendsPowerEffect(
                Tree(power.Require("effect")), cast, stateMayChange, bindingMayChange))
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
        AbilityNode Node, bool StateMayChange, bool BindingMayChange)> GuardChildren(
        AbilityNode node, Cast cast, bool stateMayChange, bool bindingMayChange,
        AbilityNode? repeatedEffect)
    {
        if (node.Kind == "forEach")
        {
            bool countWillBind = bindingMayChange && HasUnboundPowerAmount(node, cast);
            long? count = countWillBind ? null : ForEachCount(node, cast);
            if (!countWillBind
                && (stateMayChange || bindingMayChange || cast.PaymentMayMutate)
                && AmountMayChange(node.Require("count")))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' reaches a for-each count after state may change");
            }
            if (count == 0)
            {
                return [];
            }
        }

        if (node.Kind == "seq")
        {
            return Nodes(node.Argument).Select((child, index) =>
                (child, stateMayChange || index > 0, bindingMayChange));
        }
        if (node.Kind == "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select(child =>
                (child, stateMayChange || children.Count > 1, bindingMayChange));
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = stateMayChange
                || cast.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test.Argument)
                || repeatedEffect is not null
                    && RepeatedEffectCanChange(test, repeatedEffect, cast);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(value =>
                (Tree(value!), stateMayChange, bindingMayChange));
        }
        if (node.Kind is "then" or "otherwise")
        {
            var effect = Tree(node.Require("effect"));
            var dependent = Tree(node.Require(node.Kind));
            var required = node.Kind == "then"
                ? ResolutionOutcome.Full
                : ResolutionOutcome.None;
            bool answered = ActiveChoices(effect, cast).Any();
            bool dependentCanRun = stateMayChange
                || cast.PaymentMayMutate
                || bindingMayChange
                || answered
                || ResolutionOf(effect, cast) == required;
            bool predecessorMayMutate = stateMayChange
                || cast.PaymentMayMutate
                || node.Kind == "then"
                || answered;
            return dependentCanRun
                ? [
                    (effect, stateMayChange, bindingMayChange),
                    (dependent, predecessorMayMutate, bindingMayChange),
                ]
                : [(effect, stateMayChange, bindingMayChange)];
        }
        if (node.Kind is "chooseCard" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice")
        {
            return ContinuationChildren(node).Select(child =>
                (child, stateMayChange, true));
        }
        if (node.Kind is "afterActivation" or "delayUntil" or "defense"
            or "payOrEffect" or "payOrExhaust")
        {
            return ContinuationChildren(node).Select(child =>
                (child, true, bindingMayChange));
        }
        return ContinuationChildren(node).Select(child =>
            (child, stateMayChange, bindingMayChange));
    }

}

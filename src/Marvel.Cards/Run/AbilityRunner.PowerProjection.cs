using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private sealed record TracePlayerElimination(
        int? NextPlayer,
        HashSet<int> Leaving,
        HashSet<int> RelocatedCards);

    private static TracePlayerElimination PlanTracePlayerElimination(
        int player, Cast cast, HashSet<int> discarded,
        Dictionary<int, int> engagement)
    {
        int? next = null;
        for (int offset = 1; offset < cast.World.Seats.Count; offset++)
        {
            int candidate = (player + offset) % cast.World.Seats.Count;
            var identity = cast.World.Seats[candidate].IdentityCard;
            if (!cast.World.Seats[candidate].Eliminated
                && !discarded.Contains(identity.ObjectId))
            {
                next = candidate;
                break;
            }
        }

        var retained = new HashSet<int>();
        var relocated = new HashSet<int>();
        foreach (var minion in cast.World.Cards.Where(card =>
            !discarded.Contains(card.ObjectId)
            && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion))
        {
            int engagedPlayer = engagement.TryGetValue(
                    minion.ObjectId, out int traced)
                ? traced
                : minion.Area.Type == DeckType.EngagedEnemiesArea
                    ? minion.Area.PlayArea.Player
                    : -1;
            if (engagedPlayer == player && next is not null)
            {
                foreach (int relocatedCard in TraceHostedTree(minion, cast))
                {
                    relocated.Add(relocatedCard);
                }
            }
            if (engagedPlayer != player || next is not null)
            {
                foreach (int retainedCard in TraceHostedTree(minion, cast))
                {
                    retained.Add(retainedCard);
                }
            }
        }

        var leaving = cast.World.Cards
            .Where(card => card.Area.PlayArea == PlayArea.Of(player)
                && !retained.Contains(card.ObjectId))
            .Select(card => card.ObjectId)
            .ToHashSet();
        leaving.Add(cast.World.Seats[player].IdentityCard.ObjectId);
        foreach (int cardId in leaving)
        {
            var card = cast.World.Cards[cardId];
            if (DeckTypes.IsInPlay(card.Area.Type)
                && cast.World.Facts.Kind(card.FaceId) == CardKind.Attachment
                && StateFields.Modified(
                    cast.World, card, "permanent",
                    cast.World.Facts, cast.World.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"card {cardId} is a permanent attachment on an eliminated "
                    + "player's board, and rr:player-elimination.1 resolves its "
                    + "'attach to' text, which is not modelled");
            }
        }
        return new TracePlayerElimination(next, leaving, relocated);
    }

    private static List<int> TraceHostedTree(Card root, Cast cast)
    {
        var tree = new List<int> { root.ObjectId };
        var pending = new Stack<Card>(cast.World.Areas
            .Where(area => area.Host == root.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        var seen = new HashSet<int> { root.ObjectId };
        while (pending.TryPop(out var card))
        {
            if (!seen.Add(card.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {card.ObjectId} forms a hosting cycle");
            }
            tree.Add(card.ObjectId);
            foreach (var child in cast.World.Areas
                .Where(area => area.Host == card.ObjectId)
                .SelectMany(area => area.Cards)
                .Reverse())
            {
                pending.Push(child);
            }
        }
        return tree;
    }

    private static PowerReachability AdvancePowerVillain(
        PowerReachability state, Card damaged, Card first, Cast cast)
    {
        if (damaged.ObjectId != state.CurrentVillain
            || PowerDamage(state, damaged) < PowerHealth(state, damaged, cast))
        {
            return state;
        }

        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        Card? next = nextIndex >= 0 ? deck.Cards[nextIndex] : null;
        bool carriesAttachments = next is not null && string.Equals(
            cast.World.Facts.Title(damaged.FaceId),
            cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        var discarded = new HashSet<int>(state.Discarded) { damaged.ObjectId };
        if (!carriesAttachments)
        {
            foreach (int leaving in PowerLeavingTree(damaged, cast).Skip(1))
            {
                discarded.Add(leaving);
            }
        }
        if (next is not null
            && cast.Abilities is AbilityRunner runner
            && runner.On(next).Any(ability =>
                ability.Trigger.Timing == AbilityType.Constant))
        {
            // The stage is intentionally not moved while eligibility is
            // traced, so its constant abilities are absent from the unchanged
            // World. Refuse the labelled continuation before its cost mutates.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (next is not null
            && HasLiveVillainRetargetingConstant(
                discarded, damaged, next, cast,
                threatChanges: state.SchemeThreat,
                damageChanges: state.CardDamage,
                modifierChanges: state.Modifiers,
                traitChanges: state.Traits,
                statusChanges:
                [
                    .. state.StatusChanges,
                    .. state.CardTough.Keys.Select(card => (card, Statuses.Tough)),
                ],
                engagementChanges: state.Engagement,
                formsMayChange: state.FormsMayChange,
                traceFirstPlayer: state.FirstPlayer))
        {
            // A constant selector such as { query: villain } follows the new
            // stage as soon as it enters play. The unchanged World still binds
            // that effect to the defeated stage, so refuse before the labelled
            // cost rather than project the continuation from stale modifiers.
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads retargeting constant "
                + "abilities, which is not implemented");
        }
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (!carriesAttachments)
        {
            foreach (int leaving in discarded.Where(cardId =>
                cardId != damaged.ObjectId
                && !state.Discarded.Contains(cardId)))
            {
                engagement.Remove(leaving);
            }
        }
        if (nextIndex < 0)
        {
            return state with
            {
                Discarded = discarded,
                Engagement = engagement,
                CurrentVillain = -1,
                Finished = true,
            };
        }

        next = deck.Cards[nextIndex];
        bool carriesTough = string.Equals(
                cast.World.Facts.Title(damaged.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)
            && PowerTough(state, damaged, cast);
        var advanced = state with
        {
            Discarded = discarded,
            Engagement = engagement,
            CurrentVillain = next.ObjectId,
            VillainStagesDrawn = state.VillainStagesDrawn + 1,
        };
        advanced = SetPowerDamage(advanced, next, 0, first, cast);
        bool printedTough = StateFields.Modified(
            cast.World, next, "toughness",
            cast.World.Facts, cast.World.Players) > 0;
        return SetPowerTough(
            advanced, next, carriesTough || printedTough, first, cast);
    }

    private static bool ConstantCanRetargetVillain(
        AbilityNode node, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer)
    {
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                return node.Field(tracedTest ? "then" : "else") is { } traced
                    && ConstantCanRetargetVillain(
                        Tree(traced), current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange,
                        traceFirstPlayer);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer))
            {
                return StructuralChildren(node).Any(child =>
                    ConstantCanRetargetVillain(
                        child, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer));
            }
            return node.Field(Test(test, cast) ? "then" : "else") is { } active
                && ConstantCanRetargetVillain(
                    Tree(active), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer);
        }
        if (node.Kind == "grant"
            && PotentialVillainSelector(node.Require("card"), cast))
        {
            return true;
        }
        if (node.Kind == "grantEach"
            && PotentialVillainSelector(node.Require("cards"), cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ConstantCanRetargetVillain(
                child, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer));
    }

    private static bool HasLiveVillainRetargetingConstant(
        HashSet<int> discarded, Card current, Card next, Cast cast,
        IReadOnlyDictionary<int, long>? threatChanges = null,
        IReadOnlyDictionary<int, long>? damageChanges = null,
        IReadOnlyDictionary<(int Card, string Field), long>? modifierChanges = null,
        IReadOnlyDictionary<int, HashSet<string>>? traitChanges = null,
        HashSet<(int Card, string Status)>? statusChanges = null,
        IReadOnlyDictionary<int, int>? engagementChanges = null,
        ulong formsMayChange = 0, int traceFirstPlayer = -1) =>
        cast.Abilities is AbilityRunner runner
        && cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .Where(card => !discarded.Contains(card.ObjectId))
            .Any(card =>
            {
                var placement = engagementChanges ?? new Dictionary<int, int>();
                var constantCast = TraceConstantCast(cast, card, runner, placement);
                return runner.On(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant
                    && ConstantCanRetargetVillain(
                        ability.Effect, current, next, constantCast, discarded,
                        threatChanges ?? new Dictionary<int, long>(),
                        damageChanges ?? new Dictionary<int, long>(),
                        modifierChanges
                            ?? new Dictionary<(int Card, string Field), long>(),
                        traitChanges ?? new Dictionary<int, HashSet<string>>(),
                        statusChanges ?? [],
                        placement,
                        formsMayChange,
                        traceFirstPlayer < 0
                            ? cast.World.FirstPlayer
                            : traceFirstPlayer));
            });

    private static Cast TraceConstantCast(
        Cast cast, Card source, AbilityRunner runner,
        IReadOnlyDictionary<int, int> placement)
    {
        int controller = ControllerOf(cast.World, source);
        if (IsPlayerCard(cast.World.Facts, source)
            && DeckTypes.IsInPlay(source.Area.Type)
            && placement.TryGetValue(source.ObjectId, out int projectedPlayer))
        {
            controller = projectedPlayer;
        }
        return new Cast(
            cast.World, source, new Occurrence(0, []), controller, [], runner)
        {
            ProjectedPlayAreaPlayer = placement.TryGetValue(
                source.ObjectId, out int projected)
                    ? projected
                    : null,
        };
    }

    private static bool TryTraceConstantTest(
        AbilityNode test, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        out bool result)
    {
        if (test.Kind is "and" or "or")
        {
            bool unknown = false;
            foreach (var child in Nodes(test.Argument))
            {
                if (!TryTraceConstantTest(
                    child, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer,
                    out bool value))
                {
                    unknown = true;
                    continue;
                }
                if (test.Kind == "and" && !value
                    || test.Kind == "or" && value)
                {
                    result = value;
                    return true;
                }
            }
            result = test.Kind == "and";
            return !unknown;
        }
        if (test.Kind == "not")
        {
            bool known = TryTraceConstantTest(
                Tree(test.Argument), current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool inner);
            result = !inner;
            return known;
        }
        if (test.Kind == "inForm")
        {
            int seat = test.Require("player") is AbilityValue.Word
                    { Value: "firstPlayer" }
                ? traceFirstPlayer
                : Seat(test.Require("player"), cast);
            bool live = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts,
                Word(test.Require("form")));
            result = SeatMayChange(formsMayChange, seat) ? !live : live;
            return true;
        }
        if (test.Kind == "titleInPlay")
        {
            string title = Word(test.Argument);
            result = string.Equals(
                    cast.World.Facts.Title(next.FaceId), title,
                    StringComparison.Ordinal)
                || cast.World.Cards.Any(card => string.Equals(
                    cast.World.Facts.Title(card.FaceId), title,
                    StringComparison.Ordinal)
                && (DeckTypes.IsInPlay(card.Area.Type)
                    ? !discarded.Contains(card.ObjectId)
                    : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                        && !discarded.Contains(card.ObjectId)));
            return true;
        }
        if (test.Kind == "exists"
            && TraceVillainExists(test.Argument, next, cast, discarded)
                is { } exists)
        {
            result = exists;
            return true;
        }
        if (test.Kind == "exists"
            && TryTraceCount(
                test.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long existing))
        {
            result = existing > 0;
            return true;
        }
        if (test.Kind == "atLeast"
            && TryTraceCountAmount(
                test.Require("value"), next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long tracedValue)
            && TryTraceCountAmount(
                test.Require("count"), next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long tracedCount))
        {
            result = tracedValue >= tracedCount;
            return true;
        }
        if (test.Kind is "isTitle" or "isKind"
            && IsProjectedVillainSelector(test.Require("card")))
        {
            result = test.Kind == "isTitle"
                ? string.Equals(
                    cast.World.Facts.Title(next.FaceId),
                    Word(test.Require("title")),
                    StringComparison.Ordinal)
                : cast.World.Facts.Kind(next.FaceId)
                    == Kind(Word(test.Require("kind")));
            return true;
        }
        if (TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges, out result))
        {
            return true;
        }
        if (test.Kind is "hasStatus" or "hasTrait" or "isTitle" or "isKind"
            && Find(test.Require("card"), cast) is { } absentTarget
            && discarded.Contains(absentTarget.ObjectId))
        {
            result = false;
            return true;
        }
        if (!TestCanChangeOnVillainAdvance(
            test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private static bool IsProjectedVillainSelector(AbilityValue selector) =>
        selector is AbilityValue.Map { Entries.Count: 1 }
        && Tree(selector) is { Kind: "query", Argument: AbilityValue.Word
            { Value: "villain" } };

    private static bool? TraceVillainExists(
        AbilityValue selector, Card next, Cast cast,
        HashSet<int> discarded)
    {
        if (selector is not AbilityValue.Map { Entries.Count: 1 })
        {
            return null;
        }
        var node = Tree(selector);
        if (node.Kind == "query"
            && node.Argument is AbilityValue.Word
                { Value: "villain" or "enemies" or "characters" })
        {
            return true;
        }
        if (node.Kind == "titled")
        {
            string title = Word(node.Argument);
            return string.Equals(
                    cast.World.Facts.Title(next.FaceId), title,
                    StringComparison.Ordinal)
                || cast.World.Areas
                    .Where(area => DeckTypes.IsInPlay(area.Type))
                    .SelectMany(area => area.Cards)
                    .Any(card => !discarded.Contains(card.ObjectId)
                        && string.Equals(
                            cast.World.Facts.Title(card.FaceId), title,
                            StringComparison.Ordinal));
        }
        return null;
    }

    private static Card? TraceEnteredCard(
        AbilityValue selector, HashSet<int> discarded, Cast cast)
    {
        if (selector is not AbilityValue.Map { Entries.Count: 1 }
            || Tree(selector) is not { Kind: "titled" } titled)
        {
            return null;
        }
        string title = Word(titled.Argument);
        return cast.World.Cards.FirstOrDefault(card =>
            !DeckTypes.IsInPlay(card.Area.Type)
            && !discarded.Contains(card.ObjectId)
            && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
            && string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal));
    }

    private static bool TryTraceEnteredCardTest(
        AbilityNode test, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        out bool result)
    {
        result = false;
        if (test.Kind is not ("hasStatus" or "hasTrait" or "isTitle" or "isKind")
            || TraceEnteredCard(
                test.Require("card"), discarded, cast) is not { } enteredTarget)
        {
            return false;
        }
        result = test.Kind switch
        {
            "hasStatus" => statusChanges.Contains((
                enteredTarget.ObjectId,
                Word(test.Require("status")))),
            "hasTrait" => Rules.State.Traits.Has(
                    cast.World, enteredTarget,
                    Word(test.Require("trait")), cast.World.Facts)
                || traitChanges.TryGetValue(
                    enteredTarget.ObjectId, out var enteredTraits)
                    && enteredTraits.Contains(Word(test.Require("trait"))),
            "isTitle" => string.Equals(
                cast.World.Facts.Title(enteredTarget.FaceId),
                Word(test.Require("title")), StringComparison.Ordinal),
            _ => cast.World.Facts.Kind(enteredTarget.FaceId)
                == Kind(Word(test.Require("kind"))),
        };
        return true;
    }

    private static bool TraceEnteredCardTestCanChange(
        AbilityNode test, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges) =>
        TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges,
            out bool traced)
        && traced != Test(test, cast);

    private static bool TestCanChangeOnVillainAdvance(
        AbilityNode test, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth = 16) => test.Kind switch
        {
            "and" or "or" => Nodes(test.Argument).Any(child =>
                TestCanChangeOnVillainAdvance(
                    child, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)),
            "not" => TestCanChangeOnVillainAdvance(
                Tree(test.Argument), current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth),
            "inForm" => test.Require("player") is AbilityValue.Word
                    { Value: "firstPlayer" }
                ? FirstPlayerMayRebind(formsMayChange)
                    || traceFirstPlayer != cast.World.FirstPlayer
                    || SeatMayChange(formsMayChange, traceFirstPlayer)
                : SeatMayChange(
                    formsMayChange, Seat(test.Require("player"), cast)),
            "titleInPlay" => !string.Equals(
                    cast.World.Facts.Title(current.FaceId),
                    cast.World.Facts.Title(next.FaceId),
                    StringComparison.Ordinal)
                && (string.Equals(
                        Word(test.Argument),
                        cast.World.Facts.Title(current.FaceId),
                        StringComparison.Ordinal)
                    || string.Equals(
                        Word(test.Argument),
                        cast.World.Facts.Title(next.FaceId),
                        StringComparison.Ordinal))
                || TraceTitlePresenceMayDiffer(
                    Word(test.Argument), discarded, cast),
            "exists" => TraceCardsInPlayMayDiffer(discarded, cast)
                || PotentialVillainSelector(test.Argument, cast),
            "hasStatus" or "hasTrait" or "isTitle" or "isKind"
                when TraceEnteredCardTestCanChange(
                    test, cast, discarded, traitChanges, statusChanges) => true,
            "hasStatus" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } statusTarget
                    && (discarded.Contains(statusTarget.ObjectId)
                        || statusChanges.Contains((
                            statusTarget.ObjectId,
                            Word(test.Require("status"))))),
            "hasTrait" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } traitTarget
                    && (discarded.Contains(traitTarget.ObjectId)
                        || traitChanges.TryGetValue(
                            traitTarget.ObjectId, out var gainedTraits)
                            && gainedTraits.Contains(
                                Word(test.Require("trait")))),
            "isTitle" or "isKind" => PotentialVillainSelector(
                    test.Require("card"), cast)
                || Find(test.Require("card"), cast) is { } identityTarget
                    && discarded.Contains(identityTarget.ObjectId),
            "atLeast" => ValueReadsVillain(
                    test.Require("value"), cast)
                || ValueReadsVillain(test.Require("count"), cast)
                || AmountCanDifferInVillainTrace(
                    test.Require("value"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || AmountCanDifferInVillainTrace(
                    test.Require("count"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth),
            _ => false,
        };

    private static bool AmountCanDifferInVillainTrace(
        AbilityValue value, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth = 16)
    {
        if (value is AbilityValue.Number or AbilityValue.Word)
        {
            return false;
        }
        if (value is AbilityValue.List list)
        {
            return list.Values.Any(item => AmountCanDifferInVillainTrace(
                item, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth));
        }
        if (value is not AbilityValue.Map { Entries.Count: 1 })
        {
            return false;
        }

        var amount = Tree(value);
        return amount.Kind switch
        {
            "tokensOn" or "countersOn" =>
                PotentialVillainSelector(amount.Argument, cast)
                || TraceEnteredCard(amount.Argument, discarded, cast) is not null
                || Find(amount.Argument, cast) is { } tokenTarget
                    && (discarded.Contains(tokenTarget.ObjectId)
                        || threatChanges.ContainsKey(tokenTarget.ObjectId)),
            "damageOn" =>
                PotentialVillainSelector(amount.Argument, cast)
                || (Find(amount.Argument, cast)
                        ?? TraceEnteredCard(amount.Argument, discarded, cast))
                    is { } damageTarget
                    && (discarded.Contains(damageTarget.ObjectId)
                        || damageChanges.ContainsKey(damageTarget.ObjectId)),
            "remainingHealth" =>
                PotentialVillainSelector(amount.Argument, cast)
                || TraceEnteredCard(amount.Argument, discarded, cast) is not null
                || Find(amount.Argument, cast) is { } healthTarget
                    && (discarded.Contains(healthTarget.ObjectId)
                        || damageChanges.ContainsKey(healthTarget.ObjectId)
                        || modifierChanges.ContainsKey((
                            healthTarget.ObjectId, "health"))
                        || ConditionalModifierCanDiffer(
                            healthTarget, "health", current, next,
                            cast, discarded, threatChanges, damageChanges,
                            modifierChanges, traitChanges, statusChanges,
                            engagementChanges,
                            formsMayChange, traceFirstPlayer,
                            dependencyDepth)),
            "count" => TraceCountMayDiffer(
                amount.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange),
            "modified" => PotentialVillainSelector(
                    amount.Require("card"), cast)
                || TraceEnteredCard(
                    amount.Require("card"), discarded, cast) is not null
                || Find(amount.Require("card"), cast) is { } modifiedTarget
                    && (discarded.Contains(modifiedTarget.ObjectId)
                        || modifierChanges.ContainsKey((
                            modifiedTarget.ObjectId,
                            Word(amount.Require("field"))))
                        || damageChanges.ContainsKey(modifiedTarget.ObjectId)
                        || traitChanges.ContainsKey(modifiedTarget.ObjectId)
                        || statusChanges.Any(change =>
                            change.Card == modifiedTarget.ObjectId)
                        || ConditionalModifierCanDiffer(
                            modifiedTarget,
                            Word(amount.Require("field")), current, next,
                            cast, discarded, threatChanges, damageChanges,
                            modifierChanges, traitChanges, statusChanges,
                            engagementChanges,
                            formsMayChange, traceFirstPlayer,
                            dependencyDepth)),
            "min" or "add" or "mul" => Values(amount.Argument).Any(item =>
                AmountCanDifferInVillainTrace(
                    item, current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)),
            "if" => TestCanChangeOnVillainAdvance(
                    Tree(amount.Require("test")), current, next, cast,
                    discarded, threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || AmountCanDifferInVillainTrace(
                    amount.Require("then"), current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth)
                || amount.Field("else") is { } otherwise
                    && AmountCanDifferInVillainTrace(
                        otherwise, current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer, dependencyDepth),
            _ => false,
        };
    }

    private static bool ConditionalModifierCanDiffer(
        Card target, string field, Card current, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (dependencyDepth <= 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a cyclic conditional modifier "
                + "before a labelled-power continuation, which is not implemented");
        }
        return cast.Abilities is AbilityRunner runner
            && cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(source => !discarded.Contains(source.ObjectId))
                .Any(source =>
                {
                    var constantCast = TraceConstantCast(
                        cast, source, runner, engagementChanges);
                    return runner.On(source).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant
                        && ConstantFieldCanDiffer(
                            ability.Effect, source, target, field,
                            current, next, constantCast, discarded,
                            threatChanges, damageChanges, modifierChanges,
                            traitChanges, statusChanges, engagementChanges,
                            formsMayChange, traceFirstPlayer, dependencyDepth));
                });
    }

    private static bool ConstantFieldCanDiffer(
        AbilityNode node, Card source, Card target, string field,
        Card current, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (!ContainsFieldGrant(node, source, target, field, cast))
        {
            return false;
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            if (TryTraceConstantTest(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer,
                out bool tracedTest))
            {
                bool liveTest = Test(test, cast);
                if (tracedTest != liveTest)
                {
                    return node.Field(liveTest ? "then" : "else") is { } live
                            && ContainsFieldGrant(
                                Tree(live), source, target, field, cast)
                        || node.Field(tracedTest ? "then" : "else") is { } traced
                            && ContainsFieldGrant(
                                Tree(traced), source, target, field, cast);
                }
                return node.Field(tracedTest ? "then" : "else") is { } stable
                    && ConstantFieldCanDiffer(
                        Tree(stable), source, target, field,
                        current, next, cast, discarded,
                        threatChanges, damageChanges, modifierChanges,
                        traitChanges, statusChanges, engagementChanges,
                        formsMayChange, traceFirstPlayer, dependencyDepth);
            }
            if (TestCanChangeOnVillainAdvance(
                test, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer, dependencyDepth - 1))
            {
                return StructuralChildren(node).Any(child =>
                    ContainsFieldGrant(child, source, target, field, cast));
            }
            return node.Field(Test(test, cast) ? "then" : "else") is { } active
                && ConstantFieldCanDiffer(
                    Tree(active), source, target, field,
                    current, next, cast, discarded,
                    threatChanges, damageChanges, modifierChanges,
                    traitChanges, statusChanges, engagementChanges,
                    formsMayChange, traceFirstPlayer, dependencyDepth);
        }
        return StructuralChildren(node).Any(child => ConstantFieldCanDiffer(
            child, source, target, field, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer, dependencyDepth));
    }

    private static bool ContainsFieldGrant(
        AbilityNode node, Card source, Card target, string field, Cast cast)
    {
        if (node.Kind == "grant"
            && node.Field("keyword") is { } keyword
            && string.Equals(Word(keyword), field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                node.Require("card"), source, target, cast))
        {
            return true;
        }
        if (node.Kind == "grantEach"
            && node.Field("keyword") is { } eachKeyword
            && string.Equals(Word(eachKeyword), field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                node.Require("cards"), source, target, cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ContainsFieldGrant(child, source, target, field, cast));
    }

    private static bool ConstantSelectorAffects(
        AbilityValue selector, Card source, Card target, Cast cast) =>
        selector is AbilityValue.Word { Value: "this" }
            ? source.ObjectId == target.ObjectId
            : Every(selector, cast).Any(card => card.ObjectId == target.ObjectId);

    private static bool TraceCardsInPlayMayDiffer(
        HashSet<int> discarded, Cast cast) => cast.World.Cards.Any(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId));

    private static bool TraceCountMayDiffer(
        AbilityValue selector, Card next, Cast cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange)
    {
        return !TryTraceCount(
                selector, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out long traced)
            || traced != Every(selector, cast).Count;
    }

    private static bool TryTraceCountAmount(
        AbilityValue value, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        out long amount)
    {
        if (value is AbilityValue.Number number)
        {
            amount = number.Value;
            return true;
        }
        if (value is not AbilityValue.Map { Entries.Count: 1 })
        {
            amount = 0;
            return false;
        }
        var node = Tree(value);
        if (node.Kind == "count")
        {
            return TryTraceCount(
                node.Argument, next, cast, discarded,
                traitChanges, modifierChanges, engagementChanges,
                formsMayChange, out amount);
        }
        AbilityValue? target = node.Kind is "countersOn" or "modified"
            ? node.Require("card")
            : node.Kind is "tokensOn" or "damageOn" or "remainingHealth"
                ? node.Argument
                : null;
        if (target is not null
            && !PotentialVillainSelector(target, cast)
            && Find(target, cast) is { } removed
            && discarded.Contains(removed.ObjectId)
            && cast.World.Seats.Any(seat =>
                seat.IdentityCard.ObjectId == removed.ObjectId))
        {
            // Amount() returns zero when its selector no longer finds a card.
            // The trace keeps the physical World unchanged, so make that
            // runtime absence explicit after projected removal.
            amount = 0;
            return true;
        }
        if (node.Kind is "min" or "add" or "mul")
        {
            var values = new List<long>();
            foreach (var item in Values(node.Argument))
            {
                if (!TryTraceCountAmount(
                    item, next, cast, discarded,
                    traitChanges, modifierChanges, engagementChanges,
                    formsMayChange, out long traced))
                {
                    amount = 0;
                    return false;
                }
                values.Add(traced);
            }
            amount = node.Kind switch
            {
                "min" => values.Min(),
                "add" => values.Sum(),
                _ => values.Aggregate(1L, (product, item) => product * item),
            };
            return true;
        }
        amount = 0;
        return false;
    }

    private static bool TryTraceCount(
        AbilityValue selector, Card next, Cast cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        out long count)
    {
        if (selector is AbilityValue.Word { Value: "yourHero" or "yourAlterEgo" } formSelector)
        {
            int seat = Resolver(cast);
            var identity = cast.World.Seats[seat].IdentityCard;
            string form = formSelector.Value == "yourHero" ? Forms.Hero : Forms.AlterEgo;
            bool liveForm = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts, form);
            bool tracedForm = SeatMayChange(formsMayChange, seat) ? !liveForm : liveForm;
            count = tracedForm && !discarded.Contains(identity.ObjectId) ? 1 : 0;
            return true;
        }
        if (selector is AbilityValue.Map { Entries.Count: 1 }
            && Tree(selector) is { Kind: "query", Argument: AbilityValue.Word
                { Value: "heroes" } })
        {
            count = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Count(pair => !discarded.Contains(
                        pair.seat.IdentityCard.ObjectId)
                    && (SeatMayChange(formsMayChange, pair.player)
                        != Forms.In(
                            cast.World, pair.seat, cast.World.Facts,
                            Forms.Hero)));
            return true;
        }
        if (CountSelectorFormsMayChange(selector, cast, formsMayChange))
        {
            count = 0;
            return false;
        }
        var traits = traitChanges.ToDictionary(
            pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(
            modifierChanges);
        var engagement = new Dictionary<int, int>(engagementChanges);
        count = 0;
        foreach (var card in cast.World.Cards)
        {
            bool projectedInPlay = card.ObjectId == next.ObjectId
                || DeckTypes.IsInPlay(card.Area.Type)
                    && !discarded.Contains(card.ObjectId)
                || !DeckTypes.IsInPlay(card.Area.Type)
                    && FacedownDrones.Kind(card, cast.World.Facts)
                        == CardKind.Minion
                    && !discarded.Contains(card.ObjectId);
            bool projected = projectedInPlay
                && TraceSelectorMatches(
                    selector, card, next.ObjectId, cast, discarded,
                    traits, modifiers, engagement);
            if (projected)
            {
                count++;
            }
        }
        return true;
    }

    private static bool CountSelectorFormsMayChange(
        AbilityValue selector, Cast cast, ulong formsMayChange)
    {
        if (selector is AbilityValue.Word { Value: "yourHero" or "yourAlterEgo" })
        {
            return SeatMayChange(formsMayChange, Resolver(cast));
        }
        if (selector is not AbilityValue.Map)
        {
            return false;
        }
        var node = Tree(selector);
        return node.Kind == "query"
                && node.Argument is AbilityValue.Word { Value: "heroes" }
                && Enumerable.Range(0, cast.World.Seats.Count)
                    .Any(seat => SeatMayChange(formsMayChange, seat))
            || node.Kind == "withTrait"
                && CountSelectorFormsMayChange(
                    node.Require("cards"), cast, formsMayChange)
            || node.Kind is "minBy" or "maxBy"
                && CountSelectorFormsMayChange(
                    node.Require("of"), cast, formsMayChange)
            || node.Kind == "withoutAnotherCopyAttached"
                && CountSelectorFormsMayChange(
                    node.Argument, cast, formsMayChange);
    }

    private static bool TraceTitlePresenceMayDiffer(
        string title, HashSet<int> discarded, Cast cast) =>
        cast.World.Cards.Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal)
            && (DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId)));

    private static bool ValueReadsVillain(AbilityValue value, Cast cast) =>
        value is AbilityValue.Map { Entries.Count: 1 }
            && PotentialVillainSelector(value, cast)
        || value switch
        {
            AbilityValue.List list => list.Values.Any(item =>
                ValueReadsVillain(item, cast)),
            AbilityValue.Map map => map.Entries.Values.Any(item =>
                ValueReadsVillain(item, cast)),
            _ => false,
        };

    private static long PowerDamage(PowerReachability state, Card card) =>
        state.CardDamage.TryGetValue(card.ObjectId, out long damage)
            ? damage
            : card.Damage;

    private static long PowerThreat(PowerReachability state, Card scheme) =>
        TraceThreat(state.SchemeThreat, scheme);

    private static long TraceThreat(
        Dictionary<int, long> schemeThreat, Card scheme) =>
        schemeThreat.TryGetValue(scheme.ObjectId, out long threat)
            ? threat
            : scheme.Tokens.GetValueOrDefault("k_threat");

    private static bool PowerCrisis(PowerReachability state, Cast cast) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !state.Discarded.Contains(card.ObjectId))
            .Any(card => TraceModified(
                card, "crisis", cast, state.Discarded, state.Modifiers) > 0);

    private static PowerReachability SetPowerThreat(
        PowerReachability state, Card scheme, long threat)
    {
        var values = new Dictionary<int, long>(state.SchemeThreat);
        long live = scheme.Tokens.GetValueOrDefault("k_threat");
        if (threat == live)
        {
            values.Remove(scheme.ObjectId);
        }
        else
        {
            values[scheme.ObjectId] = threat;
        }
        return state with { SchemeThreat = values };
    }

    private static bool PowerTough(
        PowerReachability state, Card card, Cast cast) =>
        state.CardTough.TryGetValue(card.ObjectId, out bool tough)
            ? tough
            : Statuses.Has(cast.World, card, Statuses.Tough);

    private static PowerReadiness PowerReady(
        Card card, PowerReachability state) =>
        state.CardReadiness.TryGetValue(card.ObjectId, out var readiness)
            ? readiness
            : card.Ready
                ? PowerReadiness.Ready
                : PowerReadiness.Exhausted;

    private static PowerReachability SetPowerReady(
        PowerReachability state, Card card, PowerReadiness readiness)
    {
        var cards = new Dictionary<int, PowerReadiness>(state.CardReadiness);
        var live = card.Ready
            ? PowerReadiness.Ready
            : PowerReadiness.Exhausted;
        if (readiness == live)
        {
            cards.Remove(card.ObjectId);
        }
        else
        {
            cards[card.ObjectId] = readiness;
        }
        return state with { CardReadiness = cards };
    }

    private static long PowerCardsAvailable(
        PowerReachability state, int player, Cast cast) =>
        state.PlayerCardsAvailable.TryGetValue(player, out long available)
            ? available
            : cast.World.Seats[player].Deck.Cards.Count
                + cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;

    private static PowerReachability SetPowerCardsAvailable(
        PowerReachability state, int player, long available, Cast cast)
    {
        var cards = new Dictionary<int, long>(state.PlayerCardsAvailable);
        long live = cast.World.Seats[player].Deck.Cards.Count
            + cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;
        if (available == live)
        {
            cards.Remove(player);
        }
        else
        {
            cards[player] = available;
        }
        return state with { PlayerCardsAvailable = cards };
    }

    private static PowerReachability SetPowerDamage(
        PowerReachability state, Card card, long damage, Card first, Cast cast)
    {
        if (damage == PowerDamage(state, card))
        {
            return state;
        }
        var inventory = new Dictionary<int, long>(state.CardDamage);
        if (damage == card.Damage)
        {
            inventory.Remove(card.ObjectId);
        }
        else
        {
            inventory[card.ObjectId] = damage;
        }
        ulong forms = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        var tracedFirst = cast.World.Seats[firstPlayer].IdentityCard;
        if (card == tracedFirst
            && damage >= PowerHealth(state, tracedFirst, cast))
        {
            forms |= FirstPlayerRebinding;
            var changed = state with { CardDamage = inventory };
            for (int offset = 1; offset < cast.World.Seats.Count; offset++)
            {
                int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                var identity = cast.World.Seats[candidate].IdentityCard;
                if (PowerDamage(changed, identity) < PowerHealth(changed, identity, cast))
                {
                    firstPlayer = candidate;
                    break;
                }
            }
        }
        return state with
        {
            FormsMayChange = forms,
            FirstPlayer = firstPlayer,
            FirstPlayerDamage = card == first ? damage : state.FirstPlayerDamage,
            CardDamage = inventory,
        };
    }

    private static PowerReachability SetPowerTough(
        PowerReachability state, Card card, bool tough, Card first, Cast cast)
    {
        if (tough == PowerTough(state, card, cast))
        {
            return state;
        }
        var statuses = new Dictionary<int, bool>(state.CardTough);
        bool live = Statuses.Has(cast.World, card, Statuses.Tough);
        if (tough == live)
        {
            statuses.Remove(card.ObjectId);
        }
        else
        {
            statuses[card.ObjectId] = tough;
        }
        return state with
        {
            FirstPlayerTough = card == first ? tough : state.FirstPlayerTough,
            CardTough = statuses,
        };
    }

    private static PowerReachability MergePowerStates(
        PowerReachability left, PowerReachability right, Cast cast) =>
        MergePowerAlternatives([left, right]);

    private static PowerReachability MergePowerAlternatives(
        IEnumerable<PowerReachability> states)
    {
        var alternatives = new List<PowerReachability>();
        foreach (var state in states.SelectMany(PowerPaths))
        {
            if (!alternatives.Any(existing => SameConcretePowerState(existing, state)))
            {
                alternatives.Add(state);
            }
        }
        if (alternatives.Count == 0)
        {
            throw new InvalidOperationException("A power state must have a reachable path.");
        }
        if (alternatives.Count == 1)
        {
            return alternatives[0];
        }
        return alternatives[0] with { Alternatives = [.. alternatives] };
    }

    private static IEnumerable<PowerReachability> PowerPaths(
        PowerReachability state) => state.Alternatives ?? [state];

    private static ulong PowerForms(PowerReachability state) =>
        PowerPaths(state).Aggregate(0UL, (forms, path) => forms | path.FormsMayChange);

    private static bool SamePowerState(
        PowerReachability left, PowerReachability right)
    {
        var leftPaths = PowerPaths(left).ToList();
        var rightPaths = PowerPaths(right).ToList();
        return leftPaths.Count == rightPaths.Count
            && leftPaths.All(path => rightPaths.Any(other =>
                SameConcretePowerState(path, other)));
    }

    private static bool SameConcretePowerState(
        PowerReachability left, PowerReachability right) =>
        left.FormsMayChange == right.FormsMayChange
        && left.FirstPlayer == right.FirstPlayer
        && left.FirstPlayerDamage == right.FirstPlayerDamage
        && left.FirstPlayerTough == right.FirstPlayerTough
        && left.CardDamage.Count == right.CardDamage.Count
        && left.CardDamage.All(pair =>
            right.CardDamage.GetValueOrDefault(pair.Key) == pair.Value)
        && left.CardTough.Count == right.CardTough.Count
        && left.CardTough.All(pair =>
            right.CardTough.TryGetValue(pair.Key, out bool value)
                && value == pair.Value)
        && left.StatusChanges.SetEquals(right.StatusChanges)
        && left.StatusCounts.Count == right.StatusCounts.Count
        && left.StatusCounts.All(pair =>
            right.StatusCounts.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CardReadiness.Count == right.CardReadiness.Count
        && left.CardReadiness.All(pair =>
            right.CardReadiness.TryGetValue(pair.Key, out var value)
                && value == pair.Value)
        && left.Discarded.SetEquals(right.Discarded)
        && left.SchemeThreat.Count == right.SchemeThreat.Count
        && left.SchemeThreat.All(pair =>
            right.SchemeThreat.GetValueOrDefault(pair.Key) == pair.Value)
        && left.PlayerCardsAvailable.Count == right.PlayerCardsAvailable.Count
        && left.PlayerCardsAvailable.All(pair =>
            right.PlayerCardsAvailable.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Modifiers.Count == right.Modifiers.Count
        && left.Modifiers.All(pair =>
            right.Modifiers.GetValueOrDefault(pair.Key) == pair.Value)
        && left.Traits.Count == right.Traits.Count
        && left.Traits.All(pair =>
            right.Traits.TryGetValue(pair.Key, out var traits)
                && pair.Value.SetEquals(traits))
        && left.Engagement.Count == right.Engagement.Count
        && left.Engagement.All(pair =>
            right.Engagement.GetValueOrDefault(pair.Key, -1) == pair.Value)
        && left.CurrentVillain == right.CurrentVillain
        && left.VillainStagesDrawn == right.VillainStagesDrawn
        && left.Finished == right.Finished;

    private static long SaturatingAdd(long left, long right) =>
        right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;

    private static long SaturatingSubtract(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return long.MinValue;
        }
        if (right < 0 && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }
        return left - right;
    }

    private static ulong AllPlayerSeats(Cast cast) =>
        cast.World.Seats.Count >= 63
            ? FirstPlayerRebinding - 1
            : (1UL << cast.World.Seats.Count) - 1;

    private static ulong PlayerSeat(int seat) => 1UL << seat;

    private static bool SeatMayChange(ulong seats, int seat) =>
        (seats & PlayerSeat(seat)) != 0;

    private static IEnumerable<AbilityNode> PowerNodes(AbilityNode node, string power)
    {
        if (string.Equals(node.Kind, power.ToLowerInvariant(), StringComparison.Ordinal))
        {
            yield return node;
        }

        foreach (var found in PowerValues(node.Argument, power))
        {
            yield return found;
        }
    }

    private static IEnumerable<AbilityNode> PowerValues(AbilityValue value, string power)
    {
        if (value is AbilityValue.List list)
        {
            foreach (var found in list.Values.SelectMany(item => PowerValues(item, power)))
            {
                yield return found;
            }
            yield break;
        }

        if (value is not AbilityValue.Map map)
        {
            yield break;
        }

        if (map.Entries.Count == 1)
        {
            var node = AbilityNode.Of(value);
            foreach (var found in PowerNodes(node, power))
            {
                yield return found;
            }
            yield break;
        }

        foreach (var found in map.Entries.Values.SelectMany(item => PowerValues(item, power)))
        {
            yield return found;
        }
    }

    private static IEnumerable<AbilityNode> EachPlayers(AbilityNode node)
    {
        if (node.Kind == "eachPlayer")
        {
            yield return node;
            yield break;
        }
        IEnumerable<AbilityNode> children = node.Kind switch
        {
            "seq" or "and" => Nodes(node.Argument),
            "if" => Branches.Select(node.Field).Where(value => value is not null)
                .Select(value => Tree(value!)),
            "then" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("then")),
            ],
            "otherwise" =>
            [
                Tree(node.Require("effect")),
                Tree(node.Require("otherwise")),
            ],
            "forEach" => [Tree(node.Require("effect"))],
            _ => [],
        };
        foreach (var found in children.SelectMany(EachPlayers))
        {
            yield return found;
        }
    }

}

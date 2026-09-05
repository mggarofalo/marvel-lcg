using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    [Flags]
    private enum PowerReadiness
    {
        Ready = 1,
        Exhausted = 2,
    }

    private readonly record struct PowerReachability(
        ulong FormsMayChange, int FirstPlayer,
        long FirstPlayerDamage, bool FirstPlayerTough,
        Dictionary<int, long> CardDamage, Dictionary<int, bool> CardTough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, PowerReadiness> CardReadiness, HashSet<int> Discarded,
        Dictionary<int, long> SchemeThreat,
        Dictionary<int, long> PlayerCardsAvailable,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, HashSet<string>> Traits,
        Dictionary<int, int> Engagement,
        int CurrentVillain, int VillainStagesDrawn, bool Finished,
        PowerReachability[]? Alternatives = null);

    private static bool SuspendsPowerEffect(
        AbilityNode node, Cast cast, bool stateMayChange = false,
        bool bindingMayChange = false, PowerReachability? reachability = null)
    {
        var state = reachability ?? InitialPowerReachability(cast);
        if (state.Alternatives is { } alternatives)
        {
            return alternatives.Any(alternative => SuspendsPowerEffect(
                node, cast, stateMayChange, bindingMayChange, alternative));
        }
        return (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
            || IsChoice(node)
            || node.Kind is "eachPlayer" or "attack" or "thwart" or "thwartSchemes"
                or "placeThreat" or "enemyAttacks" or "enemySchemes"
            || PowerSuspensionChildren(
                node, cast, stateMayChange, bindingMayChange, state).Any(child =>
                SuspendsPowerEffect(
                    child.Node, cast, child.StateMayChange, child.BindingMayChange,
                    child.Reachability));
    }

    private static IEnumerable<(
        AbilityNode Node, bool StateMayChange, bool BindingMayChange,
        PowerReachability Reachability)> PowerSuspensionChildren(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (node.Kind == "seq")
        {
            var children = Nodes(node.Argument).ToList();
            var result = new List<(
                AbilityNode, bool, bool, PowerReachability)>(children.Count);
            var state = reachability;
            for (int index = 0; index < children.Count; index++)
            {
                bool mayChange = stateMayChange || index > 0;
                result.Add((children[index], mayChange, bindingMayChange, state));
                if (index + 1 < children.Count)
                {
                    state = PowerStateAfter(
                        children[index], cast, mayChange, bindingMayChange, state);
                }
            }
            return result;
        }
        if (node.Kind == "and")
        {
            var children = Nodes(node.Argument).ToList();
            return children.Select(child =>
                (child,
                    stateMayChange || children.Count > 1,
                    bindingMayChange,
                    reachability));
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = PowerTestCanChange(
                test, cast, stateMayChange, bindingMayChange, reachability);
            var branches = canSwitch
                ? Branches.Select(node.Field).Where(value => value is not null)
                : node.Field(Test(test, cast) ? "then" : "else") is { } active
                    ? [active]
                    : [];
            return branches.Select(value =>
                (Tree(value!), stateMayChange, bindingMayChange, reachability));
        }
        return GuardChildren(node, cast, stateMayChange, bindingMayChange, null)
            .Select(child =>
                (child.Node, child.StateMayChange, child.BindingMayChange,
                    reachability));
    }

    private static bool PowerTestCanChange(
        AbilityNode test, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability) => test.Kind switch
        {
            "and" or "or" => Nodes(test.Argument).Any(child =>
                PowerTestCanChange(
                    child, cast, stateMayChange, bindingMayChange, reachability)),
            "not" => PowerTestCanChange(
                Tree(test.Argument), cast, stateMayChange,
                bindingMayChange, reachability),
            "inForm" => bindingMayChange && BindingCanChange(test.Argument)
                || FirstPlayerMayRebind(PowerForms(reachability))
                    && test.Require("player") is AbilityValue.Word
                        { Value: "firstPlayer" }
                || SeatMayChange(
                    PowerForms(reachability), Seat(test.Require("player"), cast)),
            _ => stateMayChange
                || cast.PaymentMayMutate && PaymentCanChange(test)
                || bindingMayChange && BindingCanChange(test.Argument),
        };

    private static PowerReachability InitialPowerReachability(Cast cast)
    {
        var identity = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        int villain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        return new PowerReachability(
            0, cast.World.FirstPlayer, identity.Damage,
            Statuses.Has(cast.World, identity, Statuses.Tough),
            new Dictionary<int, long>(), new Dictionary<int, bool>(),
            [], [], new Dictionary<int, PowerReadiness>(), TraceUnavailableMinions(cast),
            new Dictionary<int, long>(), new Dictionary<int, long>(), [], [], [],
            villain, 0, false, null);
    }

    private static Card? PowerFind(
        AbilityValue value, Cast cast, PowerReachability reachability)
    {
        if (value is AbilityValue.Map
            && (SelectorMembershipCanChange(value)
                || PotentialVillainSelector(value, cast)))
        {
            return PowerEvery(value, cast, reachability).FirstOrDefault();
        }
        var found = Find(value, cast);
        int liveVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (found?.ObjectId == liveVillain
            && reachability.CurrentVillain != liveVillain)
        {
            found = reachability.Finished || reachability.CurrentVillain < 0
                ? null
                : cast.World.Cards[reachability.CurrentVillain];
        }
        return found is not null
            && !reachability.Discarded.Contains(found.ObjectId)
                ? found
                : null;
    }

    private static List<Card> PowerEvery(
        AbilityValue value, Cast cast, PowerReachability reachability)
    {
        int liveVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        bool dynamic = value is AbilityValue.Map
            && (SelectorMembershipCanChange(value)
                || PotentialVillainSelector(value, cast));
        if (dynamic)
        {
            var candidates = TraceCandidateCards(value, cast);
            if (liveVillain >= 0
                && PotentialVillainSelector(value, cast)
                && candidates.All(card => card.ObjectId != liveVillain))
            {
                candidates.Insert(0, cast.World.Cards[liveVillain]);
            }
            return
            [
                .. candidates.Select(card => card.ObjectId == liveVillain
                        && reachability.CurrentVillain != liveVillain
                    ? reachability.Finished || reachability.CurrentVillain < 0
                        ? null
                        : cast.World.Cards[reachability.CurrentVillain]
                    : card)
                    .Where(card => card is not null)
                    .Cast<Card>()
                    .DistinctBy(card => card.ObjectId)
                    .Where(card => !reachability.Discarded.Contains(card.ObjectId)
                        && TraceSelectorMatches(
                            value, card, reachability.CurrentVillain,
                            cast, reachability.Discarded,
                            reachability.Traits, reachability.Modifiers,
                            reachability.Engagement)),
            ];
        }
        var cards = new List<Card>();
        foreach (var found in Every(value, cast))
        {
            Card? card = found.ObjectId == liveVillain
                && reachability.CurrentVillain != liveVillain
                    ? reachability.Finished || reachability.CurrentVillain < 0
                        ? null
                        : cast.World.Cards[reachability.CurrentVillain]
                    : found;
            if (card is not null
                && !reachability.Discarded.Contains(card.ObjectId)
                && cards.All(existing => existing.ObjectId != card.ObjectId))
            {
                cards.Add(card);
            }
        }
        return cards;
    }

    private static PowerReachability PowerStateAfter(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return MergePowerAlternatives(alternatives.Select(alternative =>
                PowerStateAfter(
                    node, cast, stateMayChange, bindingMayChange, alternative)));
        }
        // SuspendsPowerEffect rejects this simultaneous choice by shape. Do
        // not eagerly replay its children while computing a later sibling's
        // abstract state; each replay would invent another damage instance.
        if (node.Kind == "and" && Nodes(node.Argument).Skip(1).Any())
        {
            return reachability;
        }
        if (node.Kind == "changeForm")
        {
            return ChangeFormState(node, cast, bindingMayChange, reachability);
        }
        if (node.Kind == "forEach")
        {
            if (HasUnboundPowerAmount(node, cast))
            {
                return MergePowerStates(
                    reachability,
                    PowerStateAfter(
                        Tree(node.Require("effect")), cast,
                        stateMayChange, bindingMayChange, reachability),
                    cast);
            }
            long count = ForEachCount(node, cast);
            var effect = Tree(node.Require("effect"));
            if (!Choices(effect).Any() && effect.Kind == "dealDamage")
            {
                return ApplyPowerLeafState(
                    effect, cast, bindingMayChange, reachability, count);
            }
            var repeated = reachability;
            for (long iteration = 0; iteration < count; iteration++)
            {
                var next = PowerStateAfter(
                    effect, cast,
                    stateMayChange || iteration > 0, bindingMayChange, repeated);
                if (SamePowerState(next, repeated))
                {
                    break;
                }
                repeated = next;
            }
            return repeated;
        }
        if (node.Kind is "then" or "otherwise")
        {
            return PowerDependentStateAfter(
                node, cast, stateMayChange, bindingMayChange, reachability);
        }

        var advanced = ApplyPowerLeafState(node, cast, bindingMayChange, reachability);
        var children = PowerSuspensionChildren(
            node, cast, stateMayChange, bindingMayChange, advanced).ToList();
        if (children.Count == 0)
        {
            return advanced;
        }
        if (node.Kind == "seq"
            || (node.Kind == "and" && children.Count == 1))
        {
            var ordered = advanced;
            foreach (var child in children)
            {
                ordered = PowerStateAfter(
                    child.Node, cast, child.StateMayChange,
                    child.BindingMayChange, child.Reachability);
            }
            return ordered;
        }

        bool includeBaseline = node.Kind != "if"
            || ConditionalCanSkipBranch(
                node, cast, stateMayChange, bindingMayChange, advanced);
        PowerReachability? merged = includeBaseline ? advanced : null;
        foreach (var child in children)
        {
            var branch = PowerStateAfter(
                child.Node, cast, child.StateMayChange,
                child.BindingMayChange, child.Reachability);
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? advanced;
    }

    private static PowerReachability PowerDependentStateAfter(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var effect = Tree(node.Require("effect"));
        var dependent = Tree(node.Require(node.Kind));
        var required = node.Kind == "then"
            ? ResolutionOutcome.Full
            : ResolutionOutcome.None;
        bool answered = ActiveChoices(effect, cast).Any();
        var outcomes = PowerOutcomeStates(
            effect, cast, stateMayChange, bindingMayChange, reachability);
        PowerReachability? merged = null;
        foreach (var outcome in outcomes)
        {
            var branch = outcome.Outcome == required
                ? PowerStateAfter(
                    dependent, cast,
                    node.Kind == "then" || answered || outcomes.Count > 1,
                    bindingMayChange, outcome.State)
                : outcome.State;
            merged = merged is { } prior
                ? MergePowerStates(prior, branch, cast)
                : branch;
        }
        return merged ?? reachability;
    }

    private readonly record struct PowerOutcomeState(
        ResolutionOutcome Outcome, PowerReachability State);

    private static List<PowerOutcomeState> PowerOutcomeStates(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return [.. alternatives.SelectMany(alternative => PowerOutcomeStates(
                node, cast, stateMayChange, bindingMayChange, alternative))];
        }
        if (node.Kind == "if")
        {
            var test = Tree(node.Require("test"));
            bool canSwitch = PowerTestCanChange(
                test, cast, stateMayChange, bindingMayChange, reachability);
            IEnumerable<AbilityValue?> branches = canSwitch
                ? Branches.Select(node.Field)
                : [node.Field(Test(test, cast) ? "then" : "else")];
            return [.. branches.SelectMany(branch => branch is null
                ? [new PowerOutcomeState(ResolutionOutcome.None, reachability)]
                : PowerOutcomeStates(
                    Tree(branch), cast, stateMayChange,
                    bindingMayChange, reachability))];
        }
        if (node.Kind == "seq")
        {
            var states = new List<PowerOutcomeState>
            {
                new(ResolutionOutcome.None, reachability),
            };
            int index = 0;
            foreach (var child in Nodes(node.Argument))
            {
                int childIndex = index++;
                states = [.. states.SelectMany(prior => PowerOutcomeStates(
                    child, cast, stateMayChange || childIndex > 0,
                    bindingMayChange, prior.State).Select(next => new PowerOutcomeState(
                        childIndex == 0
                            ? next.Outcome
                            : CombinePowerOutcomes(prior.Outcome, next.Outcome),
                        next.State)))];
            }
            return states;
        }

        var after = PowerStateAfter(
            node, cast, stateMayChange, bindingMayChange, reachability);
        var outcomes = PowerOutcomes(
            node, cast, stateMayChange, bindingMayChange, reachability);
        return [.. PowerPaths(after).SelectMany(state => outcomes.Select(outcome =>
            new PowerOutcomeState(outcome, state)))];
    }

    private static ResolutionOutcome CombinePowerOutcomes(
        ResolutionOutcome left, ResolutionOutcome right) =>
        left == right ? left : ResolutionOutcome.Partial;

    private static HashSet<ResolutionOutcome> PowerOutcomes(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        if (node.Kind == "draw")
        {
            long count = Number(node.Require("count"));
            return [CombinedOutcomes(Seats(node.Require("player"), cast).Select(
                player => ResolutionOfAmount(
                    PowerCardsAvailable(reachability, player, cast), count)))];
        }
        if (node.Kind == "heal")
        {
            var card = PowerFind(node.Require("card"), cast, reachability);
            if (card is not null)
            {
                return [ResolutionOfAmount(
                    PowerDamage(reachability, card),
                    Amount(node.Require("amount"), cast))];
            }
        }
        if (node.Kind == "discard")
        {
            AbilityValue target = node.Field("card") ?? node.Argument;
            var card = PowerFind(target, cast, reachability);
            if (card is not null)
            {
                return [reachability.Discarded.Contains(card.ObjectId)
                    ? ResolutionOutcome.None
                    : ResolutionOutcome.Full];
            }
            var unchanged = Find(target, cast);
            if (unchanged is not null
                && reachability.Discarded.Contains(unchanged.ObjectId)
                && !(bindingMayChange && BindingCanChange(target)))
            {
                return [ResolutionOutcome.None];
            }
        }
        if (node.Kind == "removeThreat")
        {
            long wanted = Amount(node.Require("amount"), cast);
            var schemes = PowerEvery(node.Require("scheme"), cast, reachability);
            var valid = schemes.Where(scheme =>
                PowerThreat(reachability, scheme) > 0
                && cast.Abilities.CanRemoveThreat(
                    cast.World, scheme, OverriddenThreatRemovalSource(node, cast))
                && (IgnoresCrisis(node)
                    || !(scheme.Area.Type == DeckType.MainSchemesArea
                    && IsPlayerCard(cast)
                    && PowerCrisis(reachability, cast))));
            return [CombinedOutcomes(valid.Select(scheme => ResolutionOfAmount(
                PowerThreat(reachability, scheme), wanted)))];
        }
        var currentTargets = node.Kind is "exhaust" or "ready"
            ? PowerEvery(node.Argument, cast, reachability)
            : [];
        bool fixedTarget = !stateMayChange
            || node.Argument is AbilityValue.Word { Value: "this" or "you" }
            || currentTargets.Count > 0
                && currentTargets.All(card => reachability.Discarded.Contains(card.ObjectId));
        if (node.Kind is "exhaust" or "ready"
            && fixedTarget
            && !(bindingMayChange && BindingCanChange(node.Argument)))
        {
            var possibilities = new HashSet<(bool Changed, bool Unchanged)>
            {
                (false, false),
            };
            foreach (var card in currentTargets.Where(card =>
                !reachability.Discarded.Contains(card.ObjectId)))
            {
                var readiness = PowerReady(card, reachability);
                bool canChange = node.Kind == "exhaust"
                    ? readiness.HasFlag(PowerReadiness.Ready)
                    : readiness.HasFlag(PowerReadiness.Exhausted);
                bool canStay = node.Kind == "exhaust"
                    ? readiness.HasFlag(PowerReadiness.Exhausted)
                    : readiness.HasFlag(PowerReadiness.Ready);
                var next = new HashSet<(bool Changed, bool Unchanged)>();
                foreach (var prior in possibilities)
                {
                    if (canChange)
                    {
                        next.Add((true, prior.Unchanged));
                    }
                    if (canStay)
                    {
                        next.Add((prior.Changed, true));
                    }
                }
                possibilities = next;
            }
            return [.. possibilities.Select(possibility => possibility switch
            {
                (false, _) => ResolutionOutcome.None,
                (true, false) => ResolutionOutcome.Full,
                _ => ResolutionOutcome.Partial,
            })];
        }
        if (node.Kind == "changeForm"
            && !(bindingMayChange && BindingCanChange(node.Require("player"))))
        {
            int seat = Seat(node.Require("player"), cast);
            bool destinationIsLive = Forms.In(
                cast.World, cast.World.Seats[seat], cast.World.Facts,
                Word(node.Require("to")));
            bool destinationIsCurrent = SeatMayChange(
                    reachability.FormsMayChange, seat)
                ? !destinationIsLive
                : destinationIsLive;
            return [destinationIsCurrent
                ? ResolutionOutcome.None
                : ResolutionOutcome.Full];
        }

        bool outcomeMayChange = stateMayChange
            || cast.PaymentMayMutate
            || bindingMayChange
            || ActiveChoices(node, cast).Any();
        return outcomeMayChange
            ? [ResolutionOutcome.None, ResolutionOutcome.Partial, ResolutionOutcome.Full]
            : [ResolutionOf(node, cast)];
    }

    private static bool ConditionalCanSkipBranch(
        AbilityNode node, Cast cast, bool stateMayChange,
        bool bindingMayChange, PowerReachability reachability)
    {
        var test = Tree(node.Require("test"));
        bool canSwitch = PowerTestCanChange(
            test, cast, stateMayChange, bindingMayChange, reachability);
        if (!canSwitch)
        {
            string active = Test(test, cast) ? "then" : "else";
            return node.Field(active) is null;
        }
        return node.Field("then") is null || node.Field("else") is null;
    }

    private static PowerReachability ChangeFormState(
        AbilityNode node, Cast cast, bool bindingMayChange,
        PowerReachability reachability)
    {
        var player = node.Require("player");
        if (bindingMayChange && BindingCanChange(player))
        {
            return reachability with
            {
                FormsMayChange = reachability.FormsMayChange | AllPlayerSeats(cast),
            };
        }
        int seat = Seat(player, cast);
        ulong bit = PlayerSeat(seat);
        bool destinationIsCurrent = Forms.In(
            cast.World, cast.World.Seats[seat], cast.World.Facts,
            Word(node.Require("to")));
        return reachability with
        {
            FormsMayChange = destinationIsCurrent
                ? reachability.FormsMayChange & ~bit
                : reachability.FormsMayChange | bit,
        };
    }

    private const ulong FirstPlayerRebinding = 1UL << 63;

    private static bool FirstPlayerMayRebind(ulong state) =>
        (state & FirstPlayerRebinding) != 0;

    private static PowerReachability ApplyPowerLeafState(
        AbilityNode node, Cast cast, bool bindingMayChange,
        PowerReachability reachability, long multiplier = 1)
    {
        if (reachability.Alternatives is { } alternatives)
        {
            return MergePowerAlternatives(alternatives.Select(alternative =>
                ApplyPowerLeafState(
                    node, cast, bindingMayChange, alternative, multiplier)));
        }
        if (node.Field("amount") is { } authoredAmount
            && (reachability.CardDamage.Count > 0
                || reachability.SchemeThreat.Count > 0)
            && AmountMayChange(authoredAmount))
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reads a mutable power amount after damage changed");
        }
        if (node.Kind is "exhaust" or "ready")
        {
            var state = reachability;
            var readiness = node.Kind == "exhaust"
                ? PowerReadiness.Exhausted
                : PowerReadiness.Ready;
            foreach (var card in PowerEvery(node.Argument, cast, reachability))
            {
                state = SetPowerReady(state, card, readiness);
            }
            return state;
        }
        if (node.Kind == "grantUntil")
        {
            var target = PowerFind(node.Require("card"), cast, reachability);
            if (target is null)
            {
                return reachability;
            }
            if (node.Field("trait") is { } gained)
            {
                var traits = reachability.Traits.ToDictionary(
                    pair => pair.Key,
                    pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
                if (!traits.TryGetValue(target.ObjectId, out var values))
                {
                    values = new HashSet<string>(StringComparer.Ordinal);
                    traits[target.ObjectId] = values;
                }
                values.Add(Word(gained));
                return reachability with { Traits = traits };
            }

            string field = Word(node.Require("keyword"));
            long grantedAmount = node.Field("amount") is { } granted
                ? Amount(granted, cast)
                : 1;
            var modifiers = new Dictionary<(int Card, string Field), long>(
                reachability.Modifiers);
            var key = (target.ObjectId, field);
            long changed = SaturatingAdd(
                modifiers.GetValueOrDefault(key), grantedAmount);
            if (changed == 0)
            {
                modifiers.Remove(key);
            }
            else
            {
                modifiers[key] = changed;
            }
            return reachability with { Modifiers = modifiers };
        }
        if (node.Kind == "putIntoPlay")
        {
            var card = Find(node.Require("card"), cast);
            if (card is null)
            {
                return reachability;
            }
            if (cast.Abilities is AbilityRunner runner
                && runner.On(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
            {
                throw new RulesNotImplementedException(
                    $"'{cast.Source.FaceId}' puts '{card.FaceId}' into play before "
                    + "a labelled-power continuation reads its constant abilities, "
                    + "which is not implemented");
            }
            var discarded = new HashSet<int>(reachability.Discarded);
            if (!discarded.Remove(card.ObjectId))
            {
                return reachability;
            }
            var engagement = new Dictionary<int, int>(reachability.Engagement);
            if (node.Field("where") is { } where
                && Word(where) == "engagedWithYou")
            {
                engagement[card.ObjectId] = Resolver(cast);
            }
            var state = reachability with
            {
                Discarded = discarded,
                Engagement = engagement,
            };
            if (StateFields.Modified(
                    cast.World, card, "toughness",
                    cast.World.Facts, cast.World.Players) > 0)
            {
                var firstIdentity = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
                state = SetPowerTough(state, card, true, firstIdentity, cast);
            }
            return state;
        }
        if (node.Kind == "draw")
        {
            long count = Number(node.Require("count"));
            var state = reachability;
            foreach (int player in Seats(node.Require("player"), cast))
            {
                long available = PowerCardsAvailable(state, player, cast);
                state = SetPowerCardsAvailable(
                    state, player, Math.Max(0, available - count), cast);
            }
            return state;
        }
        if (node.Kind == "discard")
        {
            var card = PowerFind(
                node.Field("card") ?? node.Argument, cast, reachability);
            if (card is null)
            {
                return reachability;
            }
            var discarded = new HashSet<int>(reachability.Discarded);
            var engagement = new Dictionary<int, int>(reachability.Engagement);
            var statusCounts = new Dictionary<(int Card, string Status), int>(
                reachability.StatusCounts);
            var statusChanges = new HashSet<(int Card, string Status)>(
                reachability.StatusChanges);
            foreach (int leaving in PowerLeavingTree(card, cast))
            {
                discarded.Add(leaving);
                TraceStatusesLeave(
                    leaving, cast, statusCounts, statusChanges);
                engagement.Remove(leaving);
            }
            return reachability with
            {
                Discarded = discarded,
                Engagement = engagement,
                StatusCounts = statusCounts,
                StatusChanges = statusChanges,
            };
        }
        if (node.Kind == "removeThreat")
        {
            long removedAmount = SaturatingMultiply(
                Amount(node.Require("amount"), cast), multiplier);
            var state = reachability;
            foreach (var scheme in PowerEvery(
                node.Require("scheme"), cast, reachability))
            {
                if (!cast.Abilities.CanRemoveThreat(
                        cast.World, scheme, OverriddenThreatRemovalSource(node, cast))
                    || !IgnoresCrisis(node)
                        && scheme.Area.Type == DeckType.MainSchemesArea
                        && IsPlayerCard(cast)
                        && PowerCrisis(state, cast))
                {
                    continue;
                }
                long current = PowerThreat(state, scheme);
                long changed = Math.Max(0, current - removedAmount);
                state = SetPowerThreat(state, scheme, changed);
                if (current <= 0 || changed > 0
                    || scheme.Area.Type != DeckType.SideSchemesArea)
                {
                    continue;
                }
                if (PowerDefeatHasTriggeredWork(state, scheme, cast))
                {
                    throw new RulesNotImplementedException(
                        $"side scheme '{scheme.FaceId}' is defeated before a "
                        + "labelled-power continuation reads a defeat-triggered ability, "
                        + "which is not implemented");
                }
                var discarded = new HashSet<int>(state.Discarded);
                var engagement = new Dictionary<int, int>(state.Engagement);
                foreach (int leaving in PowerLeavingTree(scheme, cast))
                {
                    discarded.Add(leaving);
                    engagement.Remove(leaving);
                }
                state = state with
                {
                    Discarded = discarded,
                    Engagement = engagement,
                };
            }
            return state;
        }
        AbilityValue? targets = node.Kind switch
        {
            "dealDamage" or "dealAttackDamage" => node.Require("cards"),
            "indirectDamage" => node.Require("among"),
            "moveDamage" or "moveAttackDamage" => node.Require("to"),
            "replaceThreatWithDamage" => node.Require("card"),
            "heal" => node.Require("card"),
            "giveStatus" => node.Require("card"),
            _ => null,
        };
        if (targets is null)
        {
            return reachability;
        }
        var first = cast.World.Seats[cast.World.FirstPlayer].IdentityCard;
        List<Card> cards = node.Kind switch
        {
            "dealDamage" or "dealAttackDamage" =>
                [.. PowerEvery(targets, cast, reachability).Where(target =>
                    CanTakeDamageInTrace(cast, target, reachability.Discarded))],
            "moveDamage" or "moveAttackDamage" =>
                PowerFind(targets, cast, reachability) is { } destination
                    && CanTakeDamageInTrace(cast, destination, reachability.Discarded)
                        ? [destination]
                        : [],
            _ => PowerEvery(targets, cast, reachability),
        };
        if (cards.Count == 0)
        {
            return bindingMayChange && BindingCanChange(targets)
                ? reachability with
                {
                    FormsMayChange = reachability.FormsMayChange
                        | FirstPlayerRebinding,
                }
                : reachability;
        }
        if (node.Kind == "heal")
        {
            long healed = Amount(node.Require("amount"), cast);
            var state = reachability;
            foreach (var card in cards)
            {
                state = SetPowerDamage(
                    state, card, Math.Max(0, PowerDamage(state, card) - healed),
                    first, cast);
            }
            return state;
        }
        if (node.Kind == "giveStatus")
        {
            string status = Word(node.Require("status"));
            if (status != Statuses.Tough)
            {
                var changes = new HashSet<(int Card, string Status)>(
                    reachability.StatusChanges);
                var counts = new Dictionary<(int Card, string Status), int>(
                    reachability.StatusCounts);
                var discarded = new HashSet<int>(reachability.Discarded);
                var engagement = new Dictionary<int, int>(reachability.Engagement);
                foreach (var card in cards)
                {
                    var key = (card.ObjectId, status);
                    int live = Statuses.Count(cast.World, card, status);
                    int current = counts.GetValueOrDefault(key, live);
                    int limit = TraceStatusLimit(
                        card, status, cast, discarded, reachability.Modifiers);
                    if (current >= limit)
                    {
                        continue;
                    }
                    int changed = current + 1;
                    TraceSetStatusCount(
                        card, status, changed, cast, counts, changes);
                    if (!TraceStatusMakesVulnerable(
                        card, status, changed, limit, cast,
                        discarded, reachability.Modifiers))
                    {
                        continue;
                    }
                    foreach (int leaving in PowerLeavingTree(card, cast))
                    {
                        discarded.Add(leaving);
                        TraceStatusesLeave(
                            leaving, cast, counts, changes);
                        engagement.Remove(leaving);
                    }
                }
                return reachability with
                {
                    StatusChanges = changes,
                    StatusCounts = counts,
                    Discarded = discarded,
                    Engagement = engagement,
                };
            }
            var state = reachability;
            foreach (var card in cards)
            {
                state = SetPowerTough(state, card, true, first, cast);
            }
            return state;
        }

        if (node.Kind is "moveDamage" or "moveAttackDamage")
        {
            var from = PowerFind(node.Require("from"), cast, reachability);
            if (from is null)
            {
                return reachability;
            }
            long moved = Math.Min(
                PowerDamage(reachability, from),
                Amount(node.Require("amount"), cast));
            var state = SetPowerDamage(
                reachability, from, PowerDamage(reachability, from) - moved,
                first, cast);
            return ApplyPowerDamage(state, cards, moved, first, cast);
        }

        long amount = SaturatingMultiply(node.Kind switch
        {
            "indirectDamage" => Amount(node.Require("amount"), cast),
            "replaceThreatWithDamage" => cast.Occurrence.Threat?.Remaining ?? 0,
            _ => Amount(node.Require("amount"), cast),
        }, multiplier);
        return ApplyPowerDamage(reachability, cards, amount, first, cast);
    }

    private static PowerReachability ApplyPowerDamage(
        PowerReachability reachability, IReadOnlyList<Card> cards,
        long amount, Card first, Cast cast)
    {
        if (amount <= 0)
        {
            return reachability;
        }
        var state = reachability;
        foreach (var card in cards)
        {
            var damage = new Dictionary<int, long>(state.CardDamage);
            var discarded = new HashSet<int>(state.Discarded);
            long landed = AfterForcedDamageReplacements(
                cast, card.ObjectId, amount, damage, discarded,
                state.CurrentVillain);
            state = state with
            {
                CardDamage = damage,
                Discarded = discarded,
            };
            if (landed <= 0)
            {
                continue;
            }
            if (PowerTough(state, card, cast))
            {
                state = SetPowerTough(state, card, false, first, cast);
                continue;
            }
            state = SetPowerDamage(
                state, card, SaturatingAdd(PowerDamage(state, card), landed),
                first, cast);
            state = ResolvePowerCharacterDefeat(state, card, first, cast);
        }
        return state;
    }

    private static PowerReachability ResolvePowerCharacterDefeat(
        PowerReachability state, Card damaged, Card first, Cast cast)
    {
        long health = PowerHealth(state, damaged, cast);
        if (PowerDamage(state, damaged) < health)
        {
            return state;
        }
        if (PowerWouldBeDefeatedHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' would be defeated before a "
                + "labelled-power continuation reads a step-6 interrupt, "
                + "which is not implemented");
        }
        if (PowerDefeatHasTriggeredWork(state, damaged, cast))
        {
            throw new RulesNotImplementedException(
                $"character '{damaged.FaceId}' is defeated before a "
                + "labelled-power continuation reads a defeat-triggered ability, "
                + "which is not implemented");
        }
        if (damaged.ObjectId == state.CurrentVillain)
        {
            return AdvancePowerVillain(state, damaged, first, cast);
        }
        if (FacedownDrones.Kind(damaged, cast.World.Facts)
            is not (CardKind.Minion or CardKind.Ally))
        {
            if (!cast.World.Seats.Any(seat => seat.IdentityCard == damaged))
            {
                return state;
            }
            int eliminatedPlayer = cast.World.Seats
                .Select((seat, player) => (seat, player))
                .Single(pair => pair.seat.IdentityCard == damaged)
                .player;
            var plan = PlanTracePlayerElimination(
                eliminatedPlayer, cast, state.Discarded, state.Engagement);
            var eliminated = new HashSet<int>(state.Discarded);
            var eliminatedEngagement = new Dictionary<int, int>(state.Engagement);
            var eliminatedStatusCounts = new Dictionary<(int Card, string Status), int>(
                state.StatusCounts);
            var eliminatedStatusChanges = new HashSet<(int Card, string Status)>(
                state.StatusChanges);
            var eliminatedTough = new Dictionary<int, bool>(state.CardTough);
            foreach (int relocated in plan.RelocatedCards)
            {
                eliminatedEngagement[relocated] = plan.NextPlayer!.Value;
            }
            foreach (int eliminatedCard in plan.Leaving)
            {
                eliminated.Add(eliminatedCard);
                eliminatedEngagement.Remove(eliminatedCard);
                eliminatedTough.Remove(eliminatedCard);
                TraceStatusesLeave(
                    eliminatedCard, cast,
                    eliminatedStatusCounts, eliminatedStatusChanges);
            }
            return state with
            {
                Discarded = eliminated,
                Engagement = eliminatedEngagement,
                CardTough = eliminatedTough,
                StatusCounts = eliminatedStatusCounts,
                StatusChanges = eliminatedStatusChanges,
            };
        }

        var leaving = PowerLeavingTree(damaged, cast);
        var discarded = new HashSet<int>(state.Discarded);
        var engagement = new Dictionary<int, int>(state.Engagement);
        var statusCounts = new Dictionary<(int Card, string Status), int>(
            state.StatusCounts);
        var statusChanges = new HashSet<(int Card, string Status)>(
            state.StatusChanges);
        foreach (int cardId in leaving)
        {
            discarded.Add(cardId);
            TraceStatusesLeave(
                cardId, cast, statusCounts, statusChanges);
            engagement.Remove(cardId);
        }
        return state with
        {
            Discarded = discarded,
            Engagement = engagement,
            StatusCounts = statusCounts,
            StatusChanges = statusChanges,
        };
    }

    private static long PowerHealth(
        PowerReachability state, Card character, Cast cast)
        => SaturatingAdd(
            TraceHealth(
                character, state.Discarded, state.SchemeThreat, cast),
            state.Modifiers.GetValueOrDefault((character.ObjectId, "health")));

    private static long TraceHealth(
        Card character, HashSet<int> discarded,
        Dictionary<int, long> schemeThreat, Cast cast)
    {
        long health = Damage.Health(cast.World, cast.World.Facts, character);
        var active = cast.World.Effects.Active()
            .Where(effect => effect.Source == EffectSource.ConstantAbility
                && string.Equals(effect.Kind, "health", StringComparison.Ordinal)
                && effect.Card is not null
                && effect.AppliesTo(cast.World, character))
            .ToList();
        var sources = active.Select(effect => effect.Card!.Value).ToHashSet();
        if (schemeThreat.Count > 0 && cast.Abilities is AbilityRunner authored)
        {
            foreach (var source in cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(card => authored.CompiledOn(card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant)))
            {
                sources.Add(source.ObjectId);
            }
        }

        foreach (int sourceId in sources)
        {
            long live = 0;
            foreach (var effect in active.Where(effect => effect.Card == sourceId))
            {
                live = SaturatingAdd(live, effect.Amount);
            }
            long traced = live;
            if (discarded.Contains(sourceId))
            {
                traced = 0;
            }
            else if (schemeThreat.Count > 0
                && cast.Abilities is AbilityRunner runner)
            {
                var source = cast.World.Cards[sourceId];
                var constantCast = new Cast(
                    cast.World, source, new Occurrence(0, []),
                    ControllerOf(cast.World, source), [], runner);
                traced = 0;
                foreach (var ability in runner.On(source).Where(ability =>
                    ability.Trigger.Timing == AbilityType.Constant))
                {
                    if (!TryTraceConstantHealth(
                        ability.Effect, character, schemeThreat,
                        constantCast, out long amount))
                    {
                        throw new RulesNotImplementedException(
                            $"character '{character.FaceId}' has a conditional health "
                            + "constant whose traced predicate is not implemented");
                    }
                    traced = SaturatingAdd(traced, amount);
                }
            }
            health = SaturatingAdd(
                SaturatingSubtract(health, live), traced);
        }
        return health;
    }

    private static bool TryTraceConstantHealth(
        AbilityNode node, Card character, Dictionary<int, long> schemeThreat,
        Cast cast, out long amount)
    {
        if (node.Kind is "seq" or "and")
        {
            amount = 0;
            foreach (var child in Nodes(node.Argument))
            {
                if (!TryTraceConstantHealth(
                    child, character, schemeThreat, cast, out long childAmount))
                {
                    return false;
                }
                amount = SaturatingAdd(amount, childAmount);
            }
            return true;
        }
        if (node.Kind == "if")
        {
            if (!TryPowerTest(
                Tree(node.Require("test")), schemeThreat, cast, out bool branch))
            {
                amount = 0;
                return false;
            }
            if (node.Field(branch ? "then" : "else") is not { } chosen)
            {
                amount = 0;
                return true;
            }
            return TryTraceConstantHealth(
                Tree(chosen), character, schemeThreat, cast, out amount);
        }
        if (node.Kind == "grant"
            && node.Field("keyword") is { } keyword
            && string.Equals(Word(keyword), "health", StringComparison.Ordinal))
        {
            if (Find(node.Require("card"), cast)?.ObjectId != character.ObjectId)
            {
                amount = 0;
                return true;
            }
            if (node.Field("amount") is { } granted)
            {
                return TryPowerAmount(granted, schemeThreat, cast, out amount);
            }
            amount = 1;
            return true;
        }
        if (node.Kind == "grantEach"
            && node.Field("keyword") is { } eachKeyword
            && string.Equals(Word(eachKeyword), "health", StringComparison.Ordinal)
            && Every(node.Require("cards"), cast).Any(card =>
                card.ObjectId == character.ObjectId))
        {
            if (node.Field("amount") is { } granted)
            {
                return TryPowerAmount(granted, schemeThreat, cast, out amount);
            }
            amount = 1;
            return true;
        }
        amount = 0;
        return true;
    }

    private static bool TryPowerTest(
        AbilityNode test, Dictionary<int, long> schemeThreat,
        Cast cast, out bool result)
    {
        if (test.Kind is "and" or "or")
        {
            var values = new List<bool>();
            foreach (var child in Nodes(test.Argument))
            {
                if (!TryPowerTest(child, schemeThreat, cast, out bool value))
                {
                    result = false;
                    return false;
                }
                values.Add(value);
            }
            result = test.Kind == "and"
                ? values.All(value => value)
                : values.Any(value => value);
            return true;
        }
        if (test.Kind == "not")
        {
            if (!TryPowerTest(
                Tree(test.Argument), schemeThreat, cast, out bool value))
            {
                result = false;
                return false;
            }
            result = !value;
            return true;
        }
        if (test.Kind == "atLeast"
            && TryPowerAmount(
                test.Require("value"), schemeThreat, cast, out long valueAt)
            && TryPowerAmount(
                test.Require("count"), schemeThreat, cast, out long count))
        {
            result = valueAt >= count;
            return true;
        }
        if (!ReadsChangedThreat(test.Argument, schemeThreat, cast))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private static bool TryPowerAmount(
        AbilityValue value, Dictionary<int, long> schemeThreat,
        Cast cast, out long amount)
    {
        if (!ReadsChangedThreat(value, schemeThreat, cast))
        {
            amount = Amount(value, cast);
            return true;
        }
        if (value is AbilityValue.Map)
        {
            var node = Tree(value);
            if (node.Kind == "tokensOn"
                && Find(node.Argument, cast) is { } scheme)
            {
                amount = TraceThreat(schemeThreat, scheme);
                return true;
            }
        }
        amount = 0;
        return false;
    }

    private static bool ReadsChangedThreat(
        AbilityValue value, Dictionary<int, long> schemeThreat,
        Cast cast) => value switch
        {
            AbilityValue.Map map => map.Entries.Any(pair =>
                string.Equals(pair.Key, "tokensOn", StringComparison.Ordinal)
                    && Find(pair.Value, cast) is { } scheme
                    && schemeThreat.ContainsKey(scheme.ObjectId)
                || ReadsChangedThreat(pair.Value, schemeThreat, cast)),
            AbilityValue.List list => list.Values.Any(item =>
                ReadsChangedThreat(item, schemeThreat, cast)),
            _ => false,
        };

    private static bool PowerWouldBeDefeatedHasTriggeredWork(
        PowerReachability state, Card defeated, Cast cast) =>
        PowerHasMatchingInterrupt(
            state, defeated, cast, Steps.CardWouldBeDefeated);

    private static bool PowerDefeatHasTriggeredWork(
        PowerReachability state, Card defeated, Cast cast) =>
        PowerHasMatchingInterrupt(state, defeated, cast, Steps.CardDefeated);

    private static bool PowerHasMatchingInterrupt(
        PowerReachability state, Card subject, Cast cast, string condition)
    {
        if (cast.Abilities is not AbilityRunner runner)
        {
            return false;
        }
        if (string.Equals(condition, Steps.CardDefeated, StringComparison.Ordinal)
            && cast.World.Facts.HasWhenDefeated(subject.FaceId)
            && !runner.On(subject).Any(ability => string.Equals(
                ability.Trigger.Event, Steps.CardDefeated,
                StringComparison.Ordinal)))
        {
            // Runtime refuses printed defeat text with no authored behavior.
            // Eligibility must make the same refusal before a labelled cost.
            return true;
        }

        // Damage steps 6 and 7 re-read every card for interrupts that answer
        // the imminent or actual defeat. Use that same board-wide matcher, but
        // omit sources the trace has already discarded.
        var occurrence = new Occurrence(
            0, [condition], Subject: subject.ObjectId, Player: subject.Owner);
        return runner.Waiting(
                cast.World, occurrence, WindowKind.Interrupt)
            .Any(pending => !state.Discarded.Contains(pending.Card));
    }

    private static List<int> PowerLeavingTree(Card host, Cast cast)
    {
        var leaving = new List<int> { host.ObjectId };
        var pending = new Stack<Card>(cast.World.Areas
            .Where(area => area.Host == host.ObjectId)
            .SelectMany(area => area.Cards)
            .Reverse());
        var seen = new HashSet<int> { host.ObjectId };
        while (pending.TryPop(out var hosted))
        {
            if (!seen.Add(hosted.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"attachment {hosted.ObjectId} forms a hosting cycle");
            }
                if (StateFields.Modified(
                        cast.World, hosted, "permanent",
                        cast.World.Facts, cast.World.Players) > 0)
            {
                throw new RulesNotImplementedException(
                    $"permanent attachment {hosted.ObjectId} lost host "
                    + $"{host.ObjectId}, and rr:permanent.5 is not implemented");
            }
            leaving.Add(hosted.ObjectId);
            foreach (var child in cast.World.Areas
                .Where(area => area.Host == hosted.ObjectId)
                .SelectMany(area => area.Cards)
                .Reverse())
            {
                pending.Push(child);
            }
        }
        return leaving;
    }

}
